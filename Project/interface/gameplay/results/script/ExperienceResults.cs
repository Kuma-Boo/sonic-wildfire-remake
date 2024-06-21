using System;
using Godot;
using Project.Core;
using Project.Gameplay;

namespace Project.Interface;

public partial class ExperienceResults : Control
{
	[Export]
	private Control expFill;
	[Export]
	private Label expLabel;
	[Export]
	private Label scoreLabel;
	/// <summary> Label for the skill bonus. </summary>
	[Export]
	private Label skillLabel;
	/// <summary> Label for the first clear bonus. </summary>
	[Export]
	private Label missionLabel;
	/// <summary> Label for the player's current level. </summary>
	[Export]
	private Label levelLabel;
	[Export]
	private Label levelGainLabel;
	[Export]
	private Label skillPointLabel;
	[Export]
	private Label skillPointGainLabel;
	[Export]
	private Label soulLabel;
	[Export]
	private Label soulGainLabel;

	[Export]
	private AnimationPlayer animator;
	private readonly string IncreaseAnimation = "increase";
	private readonly string LevelUpAnimation = "level-up";
	private readonly string ShowLevelUpAnimation = "show-level-up";

	private bool isProcessing;
	/// <summary> Amount of experience the player will have after tallying is complete. </summary>
	private int targetExp;
	/// <summary> Amount of experience when tallying started. </summary>
	private int startingExp;
	private float expInterpolation;
	private float interpolatedExp;
	private readonly float ExpSmoothing = 2.0f;

	/// <summary> Number to draw on the score label. </summary>
	private int scoreExp;
	/// <summary> Number to draw on the skill label. </summary>
	private int skillExp;
	/// <summary> Number to draw on the mission label. </summary>
	private int missionExp;
	/// <summary> Is the first clear bonus being added? </summary>
	private bool useMissionExp;

	/// <summary> Is level up data already being shown? </summary>
	private bool isLevelUpShown;
	/// <summary> Experience points needed to reach the next level. </summary>
	private int levelupRequirement;
	/// <summary> Experience points needed for the previous level up. </summary>
	private int previousLevelupRequirement;
	/// <summary> Not 100% accurate to the original game, but very close. </summary>
	private static int CalculateLevelUpRequirement(int level) => (1000 * level) + (1000 * level * (SaveManager.ActiveGameData.level / 10));

	private StageSettings Stage => StageSettings.instance;

	public override void _Ready()
	{
		useMissionExp = SaveManager.ActiveGameData.GetClearStatus(Stage.Data.LevelID) != SaveManager.GameData.LevelStatus.Cleared;
	}

	public override void _PhysicsProcess(double _)
	{
		if (!isProcessing)
			return;

		if (Input.IsActionJustPressed("button_jump") || Input.IsActionJustPressed("button_action"))
			SkipResults();

		UpdateExp();
	}

	private void UpdateExp()
	{
		if (animator.IsPlaying() && animator.CurrentAnimation != IncreaseAnimation) return;

		if (!animator.IsPlaying())
			animator.Play(IncreaseAnimation);

		float startExp = Mathf.Max(startingExp, previousLevelupRequirement);
		float endExp = Mathf.Min(levelupRequirement, targetExp);
		expInterpolation = Mathf.MoveToward(expInterpolation, 1, ExpSmoothing * PhysicsManager.physicsDelta);
		interpolatedExp = Mathf.Lerp(startExp, endExp, expInterpolation);
		SaveManager.ActiveGameData.exp = Mathf.CeilToInt(interpolatedExp);
		RedrawData();

		if (SaveManager.ActiveGameData.exp >= levelupRequirement) // Level up
		{
			SaveManager.ActiveGameData.exp = levelupRequirement;
			ProcessLevelUp();
			RedrawData();
			return;
		}

		if (SaveManager.ActiveGameData.exp == targetExp)
		{
			if (animator.CurrentAnimation == IncreaseAnimation)
				animator.Stop();

			SaveManager.SaveGameData();
			isProcessing = false;
			GetTree().CreateTimer(.5).Connect(SceneTreeTimer.SignalName.Timeout, new Callable(this, MethodName.FinishMenu));
		}
	}

	private void RedrawData()
	{
		float expRatio = (SaveManager.ActiveGameData.exp - previousLevelupRequirement) / ((float)levelupRequirement - previousLevelupRequirement);
		expFill.Scale = new(expRatio, 1);
		expLabel.Text = $"{ExtensionMethods.FormatMenuNumber(SaveManager.ActiveGameData.exp)}/{ExtensionMethods.FormatMenuNumber(levelupRequirement)}";

		int addedExp = Mathf.CeilToInt(interpolatedExp) - startingExp;
		scoreExp = Mathf.CeilToInt(Math.Clamp(Stage.TotalScore - addedExp, 0, Stage.TotalScore));
		skillExp = Mathf.CeilToInt(Math.Clamp((Stage.CurrentEXP + Stage.TotalScore) - addedExp, 0, Stage.CurrentEXP));
		if (useMissionExp)
			missionExp = Mathf.CeilToInt(Math.Clamp((Stage.CurrentEXP + Stage.TotalScore + Stage.Data.FirstClearBonus) - addedExp, 0, Stage.Data.FirstClearBonus));

		scoreLabel.Text = ExtensionMethods.FormatMenuNumber(scoreExp);
		skillLabel.Text = ExtensionMethods.FormatMenuNumber(skillExp);
		missionLabel.Text = ExtensionMethods.FormatMenuNumber(missionExp);
	}

	private void SkipResults()
	{
		// Skip any active animation
		if (animator.IsPlaying() && animator.CurrentAnimation != IncreaseAnimation)
		{
			animator.Seek(animator.CurrentAnimationLength, true, true);
			return;
		}

		// Skip everything
		SaveManager.ActiveGameData.exp = targetExp;
		ProcessLevelUp();
	}

	private void ProcessLevelUp()
	{
		int levelsGained = 0;

		while (SaveManager.ActiveGameData.exp >= levelupRequirement)
		{
			// Level up
			if (!isLevelUpShown)
			{
				isLevelUpShown = true;
				animator.Seek(animator.CurrentAnimationLength, true, true);
				animator.Play(ShowLevelUpAnimation);
				animator.Advance(0.0);
			}

			expInterpolation = 0.0f;
			levelsGained++;
			SaveManager.ActiveGameData.level++;
			previousLevelupRequirement = levelupRequirement;
			levelupRequirement = CalculateLevelUpRequirement(SaveManager.ActiveGameData.level); // Update level up requirement
		}

		if (levelsGained == 0)
			return;

		int maxSoulPower = SaveManager.ActiveGameData.CalculateMaxSoulPower();
		int maxSkillPoints = SkillRing.CalculateMaxSkillPoints(SaveManager.ActiveGameData.level);

		int soulGaugeGain = maxSoulPower - CharacterController.instance.Skills.MaxSoulPower;
		int skillPointGain = maxSkillPoints - SaveManager.ActiveGameData.skillRing.MaxSkillPoints;

		levelGainLabel.Text = $"+{levelsGained.ToString("00")}";
		skillPointGainLabel.Text = $"+{skillPointGain.ToString("000")}";
		soulGainLabel.Text = $"+{soulGaugeGain.ToString("000")}";

		SaveManager.ActiveGameData.skillRing.RefreshSkillRingData(SaveManager.ActiveGameData.level);

		levelLabel.Text = SaveManager.ActiveGameData.level.ToString("00");
		skillPointLabel.Text = maxSkillPoints.ToString("000");
		soulLabel.Text = maxSoulPower.ToString("000");

		if (animator.CurrentAnimation != ShowLevelUpAnimation)
			animator.Play(LevelUpAnimation);
	}

	private void OnResultsClosed()
	{
		levelupRequirement = CalculateLevelUpRequirement(SaveManager.ActiveGameData.level);
		previousLevelupRequirement = CalculateLevelUpRequirement(SaveManager.ActiveGameData.level - 1);

		startingExp = SaveManager.ActiveGameData.exp;
		targetExp = startingExp + Stage.TotalScore; // Add exp from score
		targetExp += Stage.CurrentEXP; // Add exp from skills
		if (useMissionExp) // Add mission bonus
			targetExp += Stage.Data.FirstClearBonus;

		RedrawData();

		// Fade to black
		TransitionManager.instance.Connect(TransitionManager.SignalName.TransitionProcess, new Callable(this, MethodName.InitializeMenu), (uint)ConnectFlags.OneShot);
		TransitionManager.instance.Connect(TransitionManager.SignalName.TransitionFinish, new Callable(this, MethodName.ShowMenu), (uint)ConnectFlags.OneShot);
		TransitionManager.StartTransition(new()
		{
			color = Colors.Black,
			inSpeed = 1f,
			outSpeed = .5f
		});
	}

	private void InitializeMenu()
	{
		animator.Play("init");
		TransitionManager.FinishTransition();
	}

	private void ShowMenu()
	{
		animator.Play("show");
		isProcessing = true;
	}

	private void FinishMenu()
	{
		// Connect the queued scene to transition signals
		TransitionManager.QueueSceneChange(TransitionManager.instance.QueuedScene);
		TransitionManager.StartTransition(new()
		{
			color = Colors.Black,
			inSpeed = .5f,
			loadAsynchronously = true,
			disableAutoTransition = string.IsNullOrEmpty(TransitionManager.instance.QueuedScene),
		});
	}
}
