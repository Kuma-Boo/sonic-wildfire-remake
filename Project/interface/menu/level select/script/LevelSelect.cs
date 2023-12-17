using Godot;
using System.Collections.Generic;

namespace Project.Interface.Menus
{
	public partial class LevelSelect : Menu
	{
		[Export]
		private string areaKey;

		[Export]
		private Control cursor;
		private int cursorPosition;
		private Vector2 cursorWidthVelocity;
		private Vector2 cursorSizeVelocity;

		[Export]
		private Control options;
		private Vector2 optionVelocity;
		private readonly List<LevelOption> levelOptions = new List<LevelOption>();
		[Export]
		private Sprite2D scrollbar;

		private int scrollAmount;
		private float scrollRatio;
		private Vector2 scrollVelocity;
		private const float SCROLL_SMOOTHING = .05f;

		[Export]
		private LevelDescription description;
		[Export]
		private ReadyMenu readyMenu;

		protected override void SetUp()
		{
			foreach (Node node in options.GetChildren())
			{
				if (node is LevelOption)
					levelOptions.Add(node as LevelOption);
			}

			if (menuMemory[MemoryKeys.ActiveMenu] == (int)MemoryKeys.LevelSelect)
				isProcessing = true;

			base.SetUp();
		}

		protected override void ProcessMenu()
		{
			base.ProcessMenu();
			UpdateListPosition(SCROLL_SMOOTHING);
		}

		public override void ShowMenu()
		{
			VerticalSelection = menuMemory[MemoryKeys.LevelSelect];
			RecalculateListPosition();
			UpdateListPosition(0);

			animator.Play("show");
			UpdateDescription();

			for (int i = 0; i < levelOptions.Count; i++)
				levelOptions[i].ShowOption();
		}
		public override void HideMenu()
		{
			for (int i = 0; i < levelOptions.Count; i++)
				levelOptions[i].HideOption();
		}


		protected override void Confirm()
		{
			if (!levelOptions[VerticalSelection].IsUnlocked)
				return;

			base.Confirm();
		}


		/// <summary> Shows the "Are you ready?" screen. </summary>
		public override void OpenSubmenu()
		{
			readyMenu.SetMapText(areaKey);
			readyMenu.SetMissionText(levelOptions[VerticalSelection].missionNameKey);
			readyMenu.parentMenu = this;
			readyMenu.LevelPath = levelOptions[VerticalSelection].levelPath;
			readyMenu.ShowMenu();
		}

		protected override void UpdateSelection()
		{
			if (Mathf.IsZeroApprox(Input.GetAxis("move_up", "move_down"))) return;

			VerticalSelection = WrapSelection(VerticalSelection + Mathf.Sign(Input.GetAxis("move_up", "move_down")), levelOptions.Count);
			menuMemory[MemoryKeys.LevelSelect] = VerticalSelection;
			animator.Play("select");
			animator.Seek(0, true);
			UpdateDescription();
			StartSelectionTimer();
			RecalculateListPosition();
		}

		private void UpdateDescription()
		{
			description.ShowDescription();
			description.SetText(levelOptions[VerticalSelection].GetDescription());
		}

		private void RecalculateListPosition()
		{
			cursorPosition = VerticalSelection;
			if (levelOptions.Count > 5)
			{
				if (VerticalSelection < 3)
				{
					scrollRatio = 0;
					scrollAmount = 0;
				}
				else if (VerticalSelection >= levelOptions.Count - 3)
				{
					scrollRatio = 1;
					scrollAmount = levelOptions.Count - 5;
					cursorPosition = 4 - ((levelOptions.Count - 1) - VerticalSelection);
				}
				else
				{
					scrollAmount = VerticalSelection - 2;
					scrollRatio = (VerticalSelection - 2) / (levelOptions.Count - 5.0f);
					cursorPosition = 2;
				}
			}
		}

		private void UpdateListPosition(float smoothing)
		{
			float targetCursorPosition = levelOptions[VerticalSelection].isSideMission ? 552 : 424;
			float targetCursorWidth = levelOptions[VerticalSelection].isSideMission ? 882 : 1012;

			cursor.Position = cursor.Position.SmoothDamp(new Vector2(targetCursorPosition, 220 + 96 * cursorPosition), ref cursorWidthVelocity, smoothing);
			cursor.Size = cursor.Size.SmoothDamp(new Vector2(targetCursorWidth, cursor.Size.Y), ref cursorSizeVelocity, smoothing);

			options.Position = options.Position.SmoothDamp(Vector2.Up * (96 * scrollAmount - 8), ref optionVelocity, smoothing);
			scrollbar.Position = scrollbar.Position.SmoothDamp(Vector2.Right * (160 * scrollRatio - 80), ref scrollVelocity, smoothing);
		}
	}
}
