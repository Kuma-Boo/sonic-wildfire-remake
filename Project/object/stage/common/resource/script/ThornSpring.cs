using Godot;
using Project.Core;

namespace Project.Gameplay.Objects
{
	[Tool]
	public partial class ThornSpring : Launcher
	{
		[Export]
		private float height;
		[Export(PropertyHint.Range, "0,5,.1")]
		/// <summary> How long rotation takes, in seconds. </summary>
		private float rotationTime;
		[Export(PropertyHint.Range, "0,5,.1")]
		/// <summary> How long to remain static, in seconds. </summary>
		private float staticTime;

		/// <summary> Is the thorn spring currently rotating? </summary>
		private bool isRotating;
		/// <summary> Current rotation ratio [0 <-> 1]. </summary>
		private float rotationRatio;
		/// <summary> How fast to rotate. </summary>
		private float rotationSpeed;

		private bool LoopRotation => (IsTimebreakSpring && !Character.Skills.IsTimeBreakActive)
			&& rotationMode != RotationMode.Launcher;

		/// <summary> How long rotations should take for inactive timebreak springs. </summary>
		private const float TIME_BREAK_ROTATION_TIME = .5f;
		/// <summary> How much to smooth rotation transitions for timebreak springs. </summary>
		private const float ROTATION_SMOOTHING = .2f;

		[Export]
		public RotationMode rotationMode;
		public enum RotationMode
		{
			Loop, //Default rotation
			Launcher, //Only rotate once when player gets launched.
			TimeBreak, //Only enable staticTime when timebreak is active.
		}

		private bool IsTimebreakSpring => rotationMode == RotationMode.TimeBreak;

		[Export]
		public Node3D root;

		public override void _PhysicsProcess(double _)
		{
			if (Engine.IsEditorHint()) return;

			UpdateRotationSpeed();
			UpdateRotationRatio();

			if (IsTimebreakSpring && !Character.Skills.IsTimeBreakActive)
			{
				if (!Character.IsLockoutActive || Character.Lockon.Target != this)
				{
					if (!isRotating)
						rotationRatio = 0;

					isRotating = true;
				}
			}

			root.Rotation = Vector3.Right * GetRotationRatio() * Mathf.Tau;
			Monitorable = !isRotating;
		}

		private void UpdateRotationRatio()
		{
			if (rotationMode == RotationMode.Launcher && !isRotating) return; //Don't rotate

			//Update timer
			if (IsTimebreakSpring && Character.Skills.IsTimeBreakActive)
				rotationRatio += rotationSpeed * PhysicsManager.physicsDelta * (float)(1.0 / Engine.TimeScale); //Unscaled time
			else
				rotationRatio += rotationSpeed * PhysicsManager.physicsDelta;

			if (rotationRatio >= 1.0f)
			{
				if (isRotating && LoopRotation)
					rotationRatio -= 1.0f;
				else
				{
					rotationRatio = 0;
					isRotating = !isRotating;
				}
			}
		}

		private void UpdateRotationSpeed()
		{
			float target = isRotating ? rotationTime : staticTime;
			if (IsTimebreakSpring && !Character.Skills.IsTimeBreakActive)
				target = TIME_BREAK_ROTATION_TIME;
			target = 1.0f / target;

			if (IsTimebreakSpring)
				rotationSpeed = Mathf.Lerp(rotationSpeed, target, ROTATION_SMOOTHING);
			else
				rotationSpeed = target;
		}

		private float GetRotationRatio()
		{
			if (isRotating)
			{
				if (LoopRotation)
					return rotationRatio;

				return Mathf.SmoothStep(0, 1, rotationRatio);
			}

			return 0;
		}

		private bool SuccessfulLaunch()
		{
			if (isRotating)
				return false;

			if (Character.Lockon.IsHomingAttacking) //Always allow homing attacks
				return true;

			if (Character.CenterPosition.Y > GlobalPosition.Y)
				return true;

			return false;
		}

		public override Vector3 GetLaunchDirection() => Vector3.Up;

		public override void Activate(Area3D a)
		{
			if (SuccessfulLaunch())
			{
				base.Activate(a);

				if (rotationMode == RotationMode.Launcher)
				{
					rotationRatio = 0;
					isRotating = true;
				}
			}
			else
				Character.StartKnockback(); //Damage the player
		}
	}
}
