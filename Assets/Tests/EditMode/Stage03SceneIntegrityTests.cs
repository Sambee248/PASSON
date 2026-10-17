using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

public sealed class Stage03SceneIntegrityTests
{
    private const string ScenePath = "Assets/Scenes/Stage03_SharedShadowCourtyards.unity";

    [Test]
    public void Stage_three_is_built_with_shared_courtyards_and_nonspawnable_shadow_alcoves()
    {
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Assert.That(sceneAsset, Is.Not.Null, "Stage 03 must be a saved scene asset.");
        var buildScenes = EditorBuildSettings.scenes;
        Assert.That(buildScenes.Length, Is.GreaterThan(3));
        Assert.That(buildScenes[3].path, Is.EqualTo(ScenePath));
        Assert.That(buildScenes[3].enabled, Is.True);

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var board = Object.FindFirstObjectByType<GridBoard>();
        Assert.That(board, Is.Not.Null);
        board.RebuildIndex();
        AssertCell(board, new Vector2Int(5, 5), true, true, false);
        AssertCell(board, new Vector2Int(17, 4), true, true, false);
        AssertCell(board, new Vector2Int(3, 6), true, true, true);
        AssertCell(board, new Vector2Int(15, 6), true, true, true);
        AssertCell(board, new Vector2Int(6, 3), false, true, false);
        AssertCell(board, new Vector2Int(18, 2), false, true, false);
        AssertCell(board, new Vector2Int(11, 9), true, false, false);

        var missing = new List<string>();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
                    missing.Add(transform.name);
        Assert.That(missing, Is.Empty);
        Assert.That(GameObject.Find("Stage03_PlateA")?.GetComponent<PressurePlate>(), Is.Not.Null);
        Assert.That(GameObject.Find("Stage03_PlateB")?.GetComponent<PressurePlate>(), Is.Not.Null);
        Assert.That(GameObject.Find("Stage03_GateA")?.GetComponent<StageGate>(), Is.Not.Null);
        Assert.That(GameObject.Find("Stage03_GateB")?.GetComponent<StageGate>(), Is.Not.Null);
        Assert.That(GameObject.Find("Stage03_RotorConsole")?.GetComponent<RotationModule>(), Is.Not.Null);
        Assert.That(GameObject.Find("Stage03_TrapConsole")?.GetComponent<EnvironmentalHazard>(), Is.Not.Null);
        var trapConsole = GameObject.Find("Stage03_TrapConsole").transform;
        Assert.That(board.TryGetCell(new Vector2Int(19, 8), out var eastRim), Is.True);
        Assert.That(trapConsole.position.x, Is.GreaterThan(eastRim.transform.position.x + 1f),
            "The trap console should sit beyond the outer rim, not block a shared walking row.");
        AssertMarker("Stage03_PlateALabel", "影子踏板");
        AssertMarker("Stage03_PlateBLabel", "影子踏板");
        AssertMarker("Stage03_TrapLabel", "陷阱机关");
        AssertMarker("Stage03_ExitLabel", "出口");
    }

    private static void AssertCell(GridBoard board, Vector2Int coordinate,
        bool player, bool shadow, bool spawnable)
    {
        Assert.That(board.TryGetCell(coordinate, out var cell), Is.True,
            $"Missing board cell {coordinate}.");
        Assert.That(cell.PlayerAccessible, Is.EqualTo(player), $"Player access at {coordinate}.");
        Assert.That(cell.ShadowWalkable, Is.EqualTo(shadow), $"Shadow access at {coordinate}.");
        Assert.That(cell.ShadowSpawnable, Is.EqualTo(spawnable), $"Shadow spawn at {coordinate}.");
    }

    private static void AssertMarker(string name, string caption)
    {
        var marker = GameObject.Find(name);
        Assert.That(marker, Is.Not.Null);
        Assert.That(marker.GetComponent<Canvas>()?.renderMode, Is.EqualTo(RenderMode.WorldSpace));
        Assert.That(marker.GetComponentInChildren<TextMeshProUGUI>()?.text,
            Does.Contain(caption));
        Assert.That(marker.GetComponentsInChildren<Collider>(), Is.Empty);
        Assert.That(marker.transform.localScale.x, Is.GreaterThan(0.017f),
            "World-space device labels must stay legible in the wide Stage03 camera.");
    }
}
