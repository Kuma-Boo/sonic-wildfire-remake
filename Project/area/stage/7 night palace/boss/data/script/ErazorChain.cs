using Godot;
using Project.Core;

namespace Project.Gameplay.Bosses
{
	[Tool]
	public partial class ErazorChain : Node3D
	{
		[Export]
		public NodePath parent;
		private ErazorChain parentChain;
		[Export]
		public NodePath child;
		private ErazorChain childChain;
		[Export]
		public float chainSize;
		[Export]
		public bool disableSimulation;

		[Export]
		public float gravity;

		public override void _EnterTree()
		{
			GetComponents();
		}

		public override void _PhysicsProcess(double _)
		{
			//Child chain or simulation is disabled
			if (disableSimulation || parentChain != null) return;

			Scale = GetParent<Node3D>().Scale; //Copy rotation from parent

			if (childChain != null)
				childChain.UpdateChain(this, gravity * PhysicsManager.physicsDelta);
		}

		private void UpdateChain(Node3D parent, float gravityAmount)
		{
			Vector3 targetPosition = GlobalPosition + Vector3.Down * gravityAmount;
			Vector3 delta = targetPosition - parent.GlobalPosition;

			if (chainSize == 0)
				targetPosition = parent.GlobalPosition;
			else
			{
				delta = delta.LimitLength(chainSize);
				targetPosition = parent.GlobalPosition + delta;
			}

			Transform3D transform = GlobalTransform;
			transform.basis.y = -delta.Normalized();
			//Rotate Chain
			transform.basis.x = parent.Back();
			transform.basis.z = parent.Right();
			transform.origin = targetPosition;
			transform = transform.Orthonormalized();
			GlobalTransform = transform;
			Scale = -parent.Scale;

			if (childChain != null)
				childChain.UpdateChain(this, gravityAmount);
		}

		private void GetComponents()
		{
			if (parent != null)
				parentChain = GetNodeOrNull<ErazorChain>(parent);

			if (child != null)
				childChain = GetNodeOrNull<ErazorChain>(child);
		}
	}
}
