using Godot;

namespace Project.Gameplay
{
	public partial class Enemy : Node3D
	{
		[Signal]
		public delegate void DefeatedEventHandler();

		[Export]
		protected CollisionShape3D collider; //Environmental collider. Disabled when defeated (For death animations, etc)
		[Export]
		protected Area3D hurtbox; //Lockon/Hitbox collider. Disabled when defeated (For death animations, etc)
		[Export]
		public int maxHealth;
		protected int currentHealth;
		[Export]
		public bool damagePlayer; //Does this enemy hurt the player on touch?

		private SpawnData spawnData;
		protected CharacterController Character => CharacterController.instance;

		protected bool IsDefeated => currentHealth <= 0;

		public override void _Ready() => SetUp();
		protected virtual void SetUp()
		{
			spawnData = new SpawnData(GetParent(), Transform);
			LevelSettings.instance.ConnectRespawnSignal(this);
			Respawn();
		}

		public override void _PhysicsProcess(double _)
		{
			if (!IsInsideTree() || !Visible) return;

			UpdateEnemy();

			if (isInteracting)
				UpdateInteraction();
		}

		public virtual void Respawn()
		{
			spawnData.Respawn(this);

			currentHealth = maxHealth;

			if (collider != null)
				collider.SetDeferred("disabled", false);
			if (hurtbox != null)
			{
				hurtbox.SetDeferred("monitorable", true);
				hurtbox.SetDeferred("monitoring", true);
			}
		}

		public virtual void Despawn()
		{
			if (!IsInsideTree()) return;
			GetParent().CallDeferred("remove_child", this);
		}

		public virtual void TakeDamage()
		{
			if (Character.Lockon.IsPerfectHomingAttack)
				currentHealth -= 2; //float damage
			else
				currentHealth--; //TODO increase player attack based on skills?

			if (IsDefeated)
				Defeat();
		}

		protected virtual void UpdateEnemy() { }

		//Called when the enemy is defeated
		protected virtual void Defeat()
		{
			//Stop colliding/monitoring
			if (collider != null)
				collider.SetDeferred("disabled", true);
			if (hurtbox != null)
			{
				hurtbox.SetDeferred("monitorable", false);
				hurtbox.SetDeferred("monitoring", false);
			}

			OnExited(null);
			EmitSignal(SignalName.Defeated);
		}

		protected bool isActivated; //Is the enemy being processed?
		protected virtual void Activate() { }
		protected virtual void Deactivate() { }

		protected bool isInteracting; //True when colliding with an object
		protected bool isInteractingWithPlayer; //True when colliding with the player specifically
		protected virtual void UpdateInteraction()
		{
			if (isInteractingWithPlayer)
			{
				if (Character.Lockon.IsBouncing) return;

				if (Character.Skills.IsSpeedBreakActive) //For now, speed break kills enemies instantly
					Defeat();
				else if (Character.ActionState == CharacterController.ActionStates.JumpDash)
				{
					TakeDamage();
					Character.Lockon.StartBounce();
				}
				else if (damagePlayer)
					Character.Knockback();
			}
		}

		public void OnEntered(Area3D a)
		{
			isInteracting = true;
			isInteractingWithPlayer = a.IsInGroup("player");
		}

		public void OnExited(Area3D _)
		{
			isInteracting = false;

			if (isInteractingWithPlayer)
				isInteractingWithPlayer = false;
		}

		public void OnRangeEntered(Area3D a)
		{
			if (!a.IsInGroup("player")) return;

			isActivated = true;
			Activate();
		}

		public void OnRangeExited(Area3D a)
		{
			if (!a.IsInGroup("player")) return;

			isActivated = false;
			Deactivate();
		}
	}
}
