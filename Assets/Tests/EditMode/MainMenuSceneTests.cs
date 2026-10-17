using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine;

public sealed class MainMenuSceneTests
{
    [Test]
    public void Player_build_starts_at_main_menu_and_keeps_all_three_stages()
    {
        var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled)
            .Select(scene => scene.path).ToArray();
        Assert.That(scenes, Is.EqualTo(new[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Stage01_OffsetBridge.unity",
            "Assets/Scenes/Stage02_TurningPursuit.unity",
            "Assets/Scenes/Stage03_SharedShadowCourtyards.unity"
        }));
    }

    [Test]
    public void Main_menu_scene_is_a_loadable_project_asset()
    {
        Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(
            "Assets/Scenes/MainMenu.unity"), Is.Not.Null);
        var scene = EditorSceneManager.OpenScene(
            "Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        Assert.That(scene.IsValid(), Is.True);
    }

    [Test]
    public void Clicking_menu_return_does_not_advance_a_completed_first_stage()
    {
        Assert.That(PuzzleHUD.ShouldConfirmCompletion(true, false, true), Is.False);
        Assert.That(PuzzleHUD.ShouldConfirmCompletion(true, false, false), Is.True);
        Assert.That(PuzzleHUD.ShouldConfirmCompletion(false, true, true), Is.True);
    }

    [Test]
    public void Menu_font_is_available_at_runtime_and_contains_chinese_glyphs()
    {
        var font = Resources.Load<TMP_FontAsset>("MenuCjk");
        Assert.That(font, Is.Not.Null);
        Assert.That(font.HasCharacter('影'), Is.True);
        Assert.That(font.HasCharacter('关'), Is.True);
    }
}
