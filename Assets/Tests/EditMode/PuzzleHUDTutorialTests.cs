using NUnit.Framework;

public sealed class PuzzleHUDTutorialTests
{
    [TestCase(Stage01TutorialStep.AlignView, "按 Z / X 切换视角，让道路对齐")]
    [TestCase(Stage01TutorialStep.PlaceShadow, "左键点格子移动；按住 F 查看可放影子的点位，松开 F 投放")]
    [TestCase(Stage01TutorialStep.HoldRemotePlate, "左键点前方格子；影子会同步跟随，送它踏上青色机关")]
    [TestCase(Stage01TutorialStep.Complete, "大门已开启，左键点终点前进")]
    [TestCase(Stage01TutorialStep.Finished, "第一关完成")]
    public void Stage_one_tutorial_steps_expose_the_authored_copy(
        Stage01TutorialStep step,
        string expectedText)
    {
        Assert.That(PuzzleHUD.TutorialTextFor(step), Is.EqualTo(expectedText));
    }

    [Test]
    public void Completion_step_has_no_active_cyan_outline()
    {
        Assert.That(PuzzleHUD.OutlineFor(Stage01TutorialStep.Complete),
            Is.EqualTo(Stage01TutorialOutline.None));
    }

    [Test]
    public void Tutorial_flow_advances_one_relevant_action_at_a_time()
    {
        var afterViewLock = PuzzleHUD.AdvanceStage01Tutorial(
            Stage01TutorialStep.AlignView,
            cameraLocked: true,
            shadowExists: false,
            remotePlatePressed: false);
        var afterShadowPlacement = PuzzleHUD.AdvanceStage01Tutorial(
            afterViewLock,
            cameraLocked: true,
            shadowExists: true,
            remotePlatePressed: false);
        var afterRemotePlate = PuzzleHUD.AdvanceStage01Tutorial(
            afterShadowPlacement,
            cameraLocked: true,
            shadowExists: true,
            remotePlatePressed: true);

        Assert.That(afterViewLock, Is.EqualTo(Stage01TutorialStep.PlaceShadow));
        Assert.That(afterShadowPlacement, Is.EqualTo(Stage01TutorialStep.HoldRemotePlate));
        Assert.That(afterRemotePlate, Is.EqualTo(Stage01TutorialStep.Complete));
    }

    [Test]
    public void Recalling_the_shadow_returns_the_tutorial_to_placement()
    {
        Assert.That(PuzzleHUD.AdvanceStage01Tutorial(
                Stage01TutorialStep.HoldRemotePlate,
                cameraLocked: true,
                shadowExists: false,
                remotePlatePressed: false),
            Is.EqualTo(Stage01TutorialStep.PlaceShadow));
    }

    [Test]
    public void Losing_the_bridge_returns_the_tutorial_to_view_selection()
    {
        Assert.That(PuzzleHUD.AdvanceStage01Tutorial(
                Stage01TutorialStep.PlaceShadow,
                cameraLocked: false,
                shadowExists: false,
                remotePlatePressed: false),
            Is.EqualTo(Stage01TutorialStep.AlignView));
    }

    [Test]
    public void Tutorial_prompts_identify_the_controls_needed_for_each_action()
    {
        Assert.That(PuzzleHUD.TutorialTextFor(Stage01TutorialStep.AlignView), Does.Contain("Z").And.Contain("X"));
        Assert.That(PuzzleHUD.TutorialTextFor(Stage01TutorialStep.PlaceShadow), Does.Contain("F"));
        Assert.That(PuzzleHUD.TutorialTextFor(Stage01TutorialStep.HoldRemotePlate),
            Does.Contain("同步跟随").And.Not.Contain("WASD"));
    }
}
