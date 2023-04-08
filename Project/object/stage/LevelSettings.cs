using Godot;
using Godot.Collections;
using System.Collections.Generic;
using Project.Core;

namespace Project.Gameplay
{
	/// <summary>
	/// Manager responsible for level setup.
	/// This is the first thing that gets loaded in a level.
	/// </summary>
	[Tool]
	public partial class LevelSettings : Node3D
	{
		#region Editor
		public override Array<Dictionary> _GetPropertyList()
		{
			Array<Dictionary> properties = new Array<Dictionary>();

			properties.Add(ExtensionMethods.CreateProperty("Level ID", Variant.Type.String));
			properties.Add(ExtensionMethods.CreateProperty("Mission Type", Variant.Type.Int, PropertyHint.Enum, MissionType.EnumToString()));
			properties.Add(ExtensionMethods.CreateProperty("Time Limit", Variant.Type.Int, PropertyHint.Range, "0,640"));

			if (MissionType != MissionTypes.None && MissionType != MissionTypes.Race)
				properties.Add(ExtensionMethods.CreateProperty("Objective Count", Variant.Type.Int, PropertyHint.Range, "0,256"));

			properties.Add(ExtensionMethods.CreateProperty("Story Event Index", Variant.Type.Int, PropertyHint.Range, "-1,30"));

			properties.Add(ExtensionMethods.CreateProperty("Item Cycling/Activation Trigger", Variant.Type.NodePath, PropertyHint.NodePathValidTypes, "Area3D"));
			if (itemCycleActivationTrigger != null && !itemCycleActivationTrigger.IsEmpty)
			{
				properties.Add(ExtensionMethods.CreateProperty("Item Cycling/Halfway Trigger", Variant.Type.NodePath, PropertyHint.NodePathValidTypes, "Area3D"));
				properties.Add(ExtensionMethods.CreateProperty("Item Cycling/Enable Respawning", Variant.Type.Bool));
				properties.Add(ExtensionMethods.CreateProperty("Item Cycling/Item Cycles", Variant.Type.Array, PropertyHint.TypeString, "22/0:"));
			}

			properties.Add(ExtensionMethods.CreateProperty("Ranking/Skip Score", Variant.Type.Bool));
			properties.Add(ExtensionMethods.CreateProperty("Ranking/Gold Time", Variant.Type.Int));
			properties.Add(ExtensionMethods.CreateProperty("Ranking/Silver Time", Variant.Type.Int));
			properties.Add(ExtensionMethods.CreateProperty("Ranking/Bronze Time", Variant.Type.Int));

			if (!skipScore)
			{
				properties.Add(ExtensionMethods.CreateProperty("Ranking/Gold Score", Variant.Type.Int, PropertyHint.Range, "0,99999999,100"));
				properties.Add(ExtensionMethods.CreateProperty("Ranking/Silver Score", Variant.Type.Int, PropertyHint.Range, "0,99999999,100"));
				properties.Add(ExtensionMethods.CreateProperty("Ranking/Bronze Score", Variant.Type.Int, PropertyHint.Range, "0,99999999,100"));
			}

			properties.Add(ExtensionMethods.CreateProperty("Completion/Delay", Variant.Type.Float, PropertyHint.Range, "0,4,.1"));
			properties.Add(ExtensionMethods.CreateProperty("Completion/Lockout", Variant.Type.Object));

			return properties;
		}

		public override Variant _Get(StringName property)
		{
			switch ((string)property)
			{
				case "Level ID":
					return (string)LevelID;

				case "Mission Type":
					return (int)MissionType;
				case "Time Limit":
					return MissionTimeLimit;
				case "Objective Count":
					return MissionObjectiveCount;
				case "Story Event Index":
					return storyEventIndex;

				case "Item Cycling/Activation Trigger":
					return itemCycleActivationTrigger;
				case "Item Cycling/Halfway Trigger":
					return itemCycleHalfwayTrigger;
				case "Item Cycling/Enable Respawning":
					return itemCycleRespawnEnabled;
				case "Item Cycling/Item Cycles":
					return itemCycles;

				case "Ranking/Skip Score":
					return skipScore;
				case "Ranking/Gold Time":
					return goldTime;
				case "Ranking/Silver Time":
					return silverTime;
				case "Ranking/Bronze Time":
					return bronzeTime;
				case "Ranking/Gold Score":
					return goldScore;
				case "Ranking/Silver Score":
					return silverScore;
				case "Ranking/Bronze Score":
					return bronzeScore;

				case "Completion/Delay":
					return completionDelay;
				case "Completion/Lockout":
					return completionLockout;
			}

			return base._Get(property);
		}

		public override bool _Set(StringName property, Variant value)
		{
			switch ((string)property)
			{
				case "Level ID":
					LevelID = (string)value;
					break;

				case "Mission Type":
					MissionType = (MissionTypes)(int)value;
					NotifyPropertyListChanged();
					break;
				case "Time Limit":
					MissionTimeLimit = (int)value;
					break;
				case "Objective Count":
					MissionObjectiveCount = (int)value;
					break;
				case "Story Event Index":
					storyEventIndex = (int)value;
					break;

				case "Item Cycling/Activation Trigger":
					itemCycleActivationTrigger = (NodePath)value;
					NotifyPropertyListChanged();
					break;
				case "Item Cycling/Halfway Trigger":
					itemCycleHalfwayTrigger = (NodePath)value;
					break;
				case "Item Cycling/Enable Respawning":
					itemCycleRespawnEnabled = (bool)value;
					break;
				case "Item Cycling/Item Cycles":
					itemCycles = (Array<NodePath>)value;
					break;

				case "Ranking/Skip Score":
					skipScore = (bool)value;
					NotifyPropertyListChanged();
					break;
				case "Ranking/Gold Time":
					goldTime = (int)value;
					break;
				case "Ranking/Silver Time":
					silverTime = (int)value;
					break;
				case "Ranking/Bronze Time":
					bronzeTime = (int)value;
					break;
				case "Ranking/Gold Score":
					goldScore = (int)value;
					break;
				case "Ranking/Silver Score":
					silverScore = (int)value;
					break;
				case "Ranking/Bronze Score":
					bronzeScore = (int)value;
					break;

				case "Completion/Delay":
					completionDelay = (float)value;
					break;
				case "Completion/Lockout":
					completionLockout = (LockoutResource)value;
					break;
				default:
					return false;
			}

			return true;
		}
		#endregion

		public static LevelSettings instance;

		public override void _EnterTree()
		{
			if (Engine.IsEditorHint()) return;

			instance = this; //Always override previous instance
			SetUpItemCycles();
		}

		public override void _PhysicsProcess(double _)
		{
			if (Engine.IsEditorHint()) return;

			UpdateTime();
		}

		#region Level Settings
		private bool skipScore; //Don't use score when ranking (i.e. for bosses)

		//Requirements for time rank. Format is in seconds.
		private int goldTime;
		private int silverTime;
		private int bronzeTime;
		//Requirement for score rank
		private int goldScore;
		private int silverScore;
		private int bronzeScore;

		/// <summary> Story event index to play after completing the stage. Leave at 0 if no story event is meant to be played. </summary>
		public int storyEventIndex = -1;

		/// <summary>
		/// Calculates the rank, from -1 <-> 3.
		/// </summary>
		public int CalculateRank()
		{
			if (LevelState == LevelStateEnum.Failed)
				return -1;

			int rank = 0; //DEFAULT - No rank

			if (skipScore)
			{
				if (CurrentTime <= goldTime)
					rank = 3;
				else if (CurrentTime <= silverTime)
					rank = 2;
				else if (CurrentTime <= bronzeTime)
					rank = 1;
			}
			else if (CurrentTime <= bronzeTime && CurrentScore >= bronzeScore)
			{
				if (CurrentTime <= goldTime && CurrentScore >= silverScore)
					rank = 3;
				else if (CurrentTime >= silverTime || CurrentScore <= silverScore)
					rank = 1;
				else
					rank = 2;
			}

			if (rank >= 3 && RespawnCount != 0) //Limit to silver if a respawn occured
				rank = 2;

			return rank;
		}

		/// <summary> Level ID, used for determining save data. </summary>
		public StringName LevelID { get; set; }

		public enum MissionTypes
		{
			None, //Add a goal node or a boss so the player doesn't get stuck!
			Objective, //Add custom nodes that call IncrementObjective()
			Ring, //Collect a certain amount of rings
			Pearl, //Collect a certain amount of pearls (normally zero)
			Enemy, //Destroy a certain amount of enemies
			Race, //Race against an enemy
		}

		/// <summary> Type of mission. </summary>
		public MissionTypes MissionType { get; private set; }
		/// <summary> What's the target amount for the mission objective? </summary>
		public int MissionObjectiveCount { get; private set; }
		/// <summary> Level time limit, in seconds. </summary>
		private float MissionTimeLimit { get; set; }
		#endregion

		#region Level Data
		public enum MathModeEnum //List of ways the score can be modified
		{
			Add,
			Subtract,
			Multiply,
			Replace
		}
		/// <summary>
		/// Calculates value based on provided MathMode.
		/// </summary>
		private int CalculateMath(int value, int amount, MathModeEnum mode)
		{
			switch (mode)
			{
				case MathModeEnum.Add:
					value += amount;
					break;
				case MathModeEnum.Subtract:
					value -= amount;
					if (value < 0) //Clamp to zero
						value = 0;
					break;
				case MathModeEnum.Multiply:
					value *= amount;
					break;
				case MathModeEnum.Replace:
					value = amount;
					break;
			}
			return value;
		}

		public int CurrentScore { get; private set; } //How high is the current score?
		public string DisplayScore { get; private set; } //Current score formatted to eight zeros
		[Signal]
		public delegate void ScoreChangedEventHandler(); //Score has changed, normally occours from a bonus

		private const string SCORE_FORMATTING = "00000000";
		public void UpdateScore(int amount, MathModeEnum mode)
		{
			CurrentScore = CalculateMath(CurrentScore, amount, mode);
			DisplayScore = CurrentScore.ToString(SCORE_FORMATTING);
			EmitSignal(SignalName.ScoreChanged);
		}

		//Bonuses
		public enum BonusType
		{
			PerfectHomingAttack, //Obtained by attacking an enemy using a perfect homing attack
			DriftBonus, //Obtained by performing a Drift
			GrindShuffle, //Obtained by performing a grind shuffle
			PerfectGrindShuffle, //Obtained by performing a perfect grind shuffle
			GrindStep, //Obtained by using the Grind Step
		}
		[Signal]
		public delegate void BonusAddedEventHandler(BonusType type);
		public void AddBonus(BonusType type)
		{
			switch (type)
			{
				default:
					break;
			}

			EmitSignal(SignalName.BonusAdded, (int)type);
		}

		public int RespawnCount { get; private set; } //How high many times did the player have to respawn?
		public void IncrementRespawnCount() => RespawnCount++;

		//Objectives
		public int CurrentObjectiveCount { get; private set; } //How much has the player currently completed?
		[Signal]
		public delegate void ObjectiveChangedEventHandler(); //Progress towards the objective has changed
		public void IncrementObjective()
		{
			CurrentObjectiveCount++;
			EmitSignal(SignalName.ObjectiveChanged);

			if (MissionObjectiveCount == 0) //i.e. Sand Oasis's "Don't break the jars!" mission.
				FinishLevel(false);
			else if (CurrentObjectiveCount >= MissionObjectiveCount)
				FinishLevel(true);
		}

		//Rings
		public int CurrentRingCount { get; private set; } //How many rings is the player currently holding?
		[Signal]
		public delegate void RingChangedEventHandler(int change); //Ring count has changed
		public void UpdateRingCount(int amount, MathModeEnum mode, bool disableAnimations = false)
		{
			int previousAmount = CurrentRingCount;
			CurrentRingCount = CalculateMath(CurrentRingCount, amount, mode);
			if (MissionType == MissionTypes.Ring && CurrentRingCount >= MissionObjectiveCount) //For ring based missions
			{
				CurrentRingCount = MissionObjectiveCount; //Clamp
				FinishLevel(true);
			}

			EmitSignal(SignalName.RingChanged, CurrentRingCount - previousAmount, disableAnimations);
		}

		//Time
		[Signal]
		public delegate void TimeChangedEventHandler(); //Time has changed.

		public float CurrentTime { get; private set; } //How long has the player been on this level? (In Seconds)
		public string DisplayTime { get; private set; } //Current time formatted in mm:ss.ff

		private const string TIME_LABEL_FORMAT = "mm':'ss'.'ff";
		private void UpdateTime()
		{
			if (IsLevelFinished || Interface.Countdown.IsCountdownActive) return;

			CurrentTime += PhysicsManager.physicsDelta; //Add current time
			if (MissionTimeLimit == 0) //No time limit
			{
				System.TimeSpan time = System.TimeSpan.FromSeconds(CurrentTime);
				DisplayTime = time.ToString(TIME_LABEL_FORMAT);
			}
			else
			{
				System.TimeSpan time = System.TimeSpan.FromSeconds(Mathf.Clamp(MissionTimeLimit - CurrentTime, 0, MissionTimeLimit));
				DisplayTime = time.ToString(TIME_LABEL_FORMAT);
				if (CurrentTime >= MissionTimeLimit) //Time's up!
					FinishLevel(false);
			}

			EmitSignal(SignalName.TimeChanged);
		}
		#endregion

		#region Level Completion
		public LockoutResource completionLockout;
		public enum LevelStateEnum
		{
			Incomplete,
			Failed,
			Success,
		}
		public LevelStateEnum LevelState { get; private set; }
		private bool IsLevelFinished => LevelState != LevelStateEnum.Incomplete;
		private float completionDelay;

		[Signal]
		public delegate void LevelCompletedEventHandler(); //Called when level is completed
		public void FinishLevel(bool wasSuccessful)
		{
			if (StageSettings.instance.StartCompletionDemo()) //Attempt to start the completion demo
			{
				//Hide everything so shadows don't render
				Visible = false;

				//Disable all object nodes that aren't parented to this node
				Array<Node> nodes = GetTree().GetNodesInGroup("cull on complete");
				for (int i = 0; i < nodes.Count; i++)
				{
					if (nodes[i] is Node3D node)
						node.Visible = false;
				}
			}

			LevelState = wasSuccessful ? LevelStateEnum.Success : LevelStateEnum.Failed;
			EmitSignal(SignalName.LevelCompleted);
		}

		#endregion

		#region Object Spawning
		//Checkpoint data
		public Triggers.CheckpointTrigger CurrentCheckpoint { get; private set; }
		public Path3D CheckpointPlayerPath { get; private set; }
		public Path3D CheckpointCameraPath { get; private set; }
		public CameraSettingsResource CheckpointCameraSettings;
		public void SetCheckpoint(Triggers.CheckpointTrigger newCheckpoint)
		{
			if (newCheckpoint == CurrentCheckpoint) return; //Already at this checkpoint

			CurrentCheckpoint = newCheckpoint; //Position transform
			CheckpointPlayerPath = CharacterController.instance.PathFollower.ActivePath; //Store current path
			CheckpointCameraPath = CameraController.instance.PathFollower.ActivePath; //Store current path
			CheckpointCameraSettings = CameraController.instance.ActiveSettings;

			EmitSignal(SignalName.OnTriggeredCheckpoint);
		}
		[Signal]
		public delegate void OnTriggeredCheckpointEventHandler();

		[Signal]
		public delegate void OnUnloadedEventHandler();
		private const string UNLOAD_FUNCTION = "Unload"; //Clean up any memory leaks in this function
		public override void _ExitTree() => EmitSignal(SignalName.OnUnloaded);
		public void ConnectUnloadSignal(Node node)
		{
			if (!node.HasMethod(UNLOAD_FUNCTION))
			{
				GD.PrintErr($"Node {node.Name} doesn't have a function '{UNLOAD_FUNCTION}!'");
				return;
			}

			if (!IsConnected(SignalName.OnUnloaded, new Callable(node, UNLOAD_FUNCTION)))
				Connect(SignalName.OnUnloaded, new Callable(node, UNLOAD_FUNCTION));
		}

		[Signal]
		public delegate void OnRespawnedEventHandler();
		private const string RESPAWN_FUNCTION = "Respawn"; //Default name of respawn functions
		public void ConnectRespawnSignal(Node node)
		{
			if (!node.HasMethod(RESPAWN_FUNCTION))
			{
				GD.PrintErr($"Node {node.Name} doesn't have a function '{RESPAWN_FUNCTION}!'");
				return;
			}

			if (!IsConnected(SignalName.OnRespawned, new Callable(node, RESPAWN_FUNCTION)))
				Connect(SignalName.OnRespawned, new Callable(node, RESPAWN_FUNCTION), (uint)ConnectFlags.Deferred);
		}

		public void RespawnObjects()
		{
			SoundManager.instance.CancelDialog(); //Cancel any active dialog
			EmitSignal(SignalName.OnRespawned);
		}

		private bool itemCycleRespawnEnabled = true; //Respawn items when cycling?
		private bool itemCycleFlagSet; //Should we trigger an item switch?
		private int itemCycleIndex; //Active item set

		//Make sure itemCycle triggers are monitoring and collide with the player
		private NodePath itemCycleActivationTrigger; //When to apply the item cycle
		private NodePath itemCycleHalfwayTrigger; //So the player can't just move in and out of the activation trigger
		private Array<NodePath> itemCycles = new Array<NodePath>();
		private readonly List<Node3D> _itemCycles = new List<Node3D>();
		private readonly List<SpawnData> _itemCyclesSpawnData = new List<SpawnData>();

		private void SetUpItemCycles()
		{
			if (itemCycleActivationTrigger == null || itemCycleActivationTrigger.IsEmpty) return; //Item cycling disabled

			GetNode<Area3D>(itemCycleActivationTrigger).Connect(Area3D.SignalName.AreaEntered, new Callable(this, MethodName.OnItemCycleActivate));
			if (itemCycleHalfwayTrigger != null)
				GetNode<Area3D>(itemCycleHalfwayTrigger).Connect(Area3D.SignalName.AreaEntered, new Callable(this, MethodName.OnItemCycleHalfwayEntered));

			for (int i = 0; i < itemCycles.Count; i++)
			{
				if (itemCycles[i] == null || itemCycles[i].IsEmpty) //Nothing on this item cycle!
				{
					_itemCycles.Add(null);
					_itemCyclesSpawnData.Add(new SpawnData());
					continue;
				}

				Node3D node = GetNode<Node3D>(itemCycles[i]);
				SpawnData spawnData = new SpawnData(node.GetParent(), node.Transform);
				node.Visible = true;

				_itemCycles.Add(node);
				_itemCyclesSpawnData.Add(spawnData);

				if (i != itemCycleIndex) //Disable inactive nodes
					spawnData.parentNode.CallDeferred(MethodName.RemoveChild, node);
			}
		}

		public void OnItemCycleActivate(Area3D a)
		{
			if (!a.IsInGroup("player")) return;
			if (itemCycles.Count == 0 || !itemCycleFlagSet) return;

			//Cycle items
			if (itemCycleRespawnEnabled)
				RespawnObjects();

			if (_itemCycles[itemCycleIndex] != null) //Despawn current item cycle
				_itemCyclesSpawnData[itemCycleIndex].parentNode.CallDeferred(MethodName.RemoveChild, _itemCycles[itemCycleIndex]);

			//Increment counter
			itemCycleIndex++;
			if (itemCycleIndex > itemCycles.Count - 1)
				itemCycleIndex = 0;

			if (_itemCycles[itemCycleIndex] != null) //Spawn current item cycle
			{
				_itemCyclesSpawnData[itemCycleIndex].parentNode.AddChild(_itemCycles[itemCycleIndex]);
				_itemCycles[itemCycleIndex].Transform = _itemCyclesSpawnData[itemCycleIndex].spawnTransform;
			}

			itemCycleFlagSet = false;
		}

		public void OnItemCycleHalfwayEntered(Area3D a)
		{
			if (!a.IsInGroup("player")) return;
			itemCycleFlagSet = true;
		}
		#endregion
	}

	public struct SpawnData
	{
		public Node parentNode; //Original parent node
		public Transform3D spawnTransform; //Local transform to spawn with
		public SpawnData(Node parent, Transform3D transform)
		{
			parentNode = parent;
			spawnTransform = transform;
		}

		public void Respawn(Node n)
		{
			if (n.GetParent() != parentNode)
			{
				if (n.IsInsideTree()) //Object needs to be reparented first.
					n.GetParent().RemoveChild(n);

				parentNode.CallDeferred(Node.MethodName.AddChild, n);
			}

			n.SetDeferred("transform", spawnTransform);
		}
	}
}