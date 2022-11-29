using Godot;
using Godot.Collections;

namespace Project.Gameplay
{
	/// <summary>
	/// Renders planar reflections into a global texture. Note that this method is hard-coded to only support reflections facing Vector3.Up
	/// </summary>
	[Tool]
	public partial class PlanarReflectionManager : Node3D
	{
		[Export]
		private NodePath reflectionCameraPath;
		private Camera3D reflectionCamera;
		private Camera3D mainCamera;
		[Export]
		private NodePath reflectionViewportPath;
		private SubViewport reflectionViewport;

		[Export]
		public Array<ShaderMaterial> reflectionMaterials; //List of materials that use reflection_texture

		[Export]
		private Node3D reflectorNode; //Plane to reflect against. Must be facing up
		[Export]
		private float reflectorHeight; //Alternatively, specify reflection Y point here
		private Texture2D reflectionTexture;
		private Vector3 previousCapturePosition;

		public override void _EnterTree()
		{
			if (GetReflectionViewport())
				reflectionViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
		}

		public override void _Process(double _)
		{
			if (!IsVisibleInTree()) return;

			UpdatePosition();
			RenderTexture();
		}

		private bool GetCamera()
		{
			if (reflectionCamera == null)
				reflectionCamera = GetNodeOrNull<Camera3D>(reflectionCameraPath);

			if (Engine.IsEditorHint())
				mainCamera = Editor.Plugin.editorCam;
			else if (CameraController.instance != null)
				mainCamera = CameraController.instance.Camera;

			return mainCamera != null && reflectionCamera != null;
		}

		//Mirror main camera along plane
		private void UpdatePosition()
		{
			if (!GetCamera()) return;

			reflectionCamera.Fov = mainCamera.Fov;
			reflectionCamera.Size = mainCamera.Size;
			reflectionCamera.Projection = mainCamera.Projection;

			//Update reflectionCamera's position
			float reflectionHeight = reflectorHeight;
			if (reflectorNode != null)
				reflectionHeight = reflectorNode.GlobalPosition.y;
			Vector3 projection = Vector3.Down * (mainCamera.GlobalPosition.y - reflectionHeight);
			Vector3 targetPosition = mainCamera.GlobalPosition + projection * 2f;

			//Update reflectionCamera's rotation
			Vector3 upDirection = mainCamera.Up().Reflect(Vector3.Up);
			Vector3 forwardDirection = mainCamera.Forward().Reflect(Vector3.Up);

			reflectionCamera.LookAtFromPosition(targetPosition, targetPosition + forwardDirection, upDirection);
		}

		private bool GetReflectionViewport()
		{
			if (reflectionViewportPath == null) return false;
			if (reflectionViewport == null)
				reflectionViewport = GetNodeOrNull<SubViewport>(reflectionViewportPath);

			return reflectionViewport != null;
		}

		private void RenderTexture()
		{
			if (!GetReflectionViewport()) return;


			if (Engine.IsEditorHint())
			{
				if (reflectionCamera.GlobalPosition.IsEqualApprox(previousCapturePosition)) return; //Didn't move
				previousCapturePosition = reflectionCamera.GlobalPosition;
			}

			reflectionViewport.Size = new Vector2i(1920, 1080) / 2;
			reflectionViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;

			reflectionTexture = reflectionViewport.GetTexture();

			for (int i = 0; i < reflectionMaterials.Count; i++)
			{
				reflectionMaterials[i].SetShaderParameter("reflection_texture", reflectionTexture);
			}
		}
	}
}