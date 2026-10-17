using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class Stage01GridAuthoring
{
    private const string ScenePath = "Assets/Scenes/Stage01_OffsetBridge.unity";
    private const float Pitch = 1.5f;
    private const float FloorY = 1f;

    [MenuItem("Tools/Stage01/Build Square Grid Stage")]
    public static void Build()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        var bridge = Required("LockableBridge");
        var gate = Required("Stage01_T7_FirstBrassGate");
        var player = Required("Stage01_T7_Player");
        var graph = Required("Stage01_PathGraph").GetComponent<PathGraph>();
        if (graph == null) throw new InvalidOperationException("Stage01 path graph is missing.");

        var boardRoot = GameObject.Find("Stage01_GridBoard");
        if (boardRoot == null) boardRoot = new GameObject("Stage01_GridBoard");
        var board = boardRoot.GetComponent<GridBoard>();
        if (board == null) board = boardRoot.AddComponent<GridBoard>();
        var playerStone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Stage01_StoneGray.mat");
        var shadowStone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ShadowInk.mat");
        var foundationStone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Stage01Foundation.mat");
        if (foundationStone == null)
        {
            var shader = Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("The Standard shader is missing.");
            foundationStone = new Material(shader) { name = "Stage01Foundation",
                color = new Color(0.09f, 0.13f, 0.18f) };
            AssetDatabase.CreateAsset(foundationStone, "Assets/Materials/Stage01Foundation.mat");
        }
        if (playerStone == null || shadowStone == null)
        {
            throw new InvalidOperationException("Stage01 tile materials are missing.");
        }

        var cells = new List<GridCell>();
        var nodes = new List<PathNode>();
        for (var x = 0; x <= 17; x++)
        {
            GridCell cell;
            if (x == 6)
            {
                Resize(bridge, Center(x, 0) - Vector3.up * 0.1f,
                    new Vector3(Pitch, 0.2f, Pitch));
                bridge.layer = LayerMask.NameToLayer("Walkable");
                var surface = bridge.GetComponent<BoxCollider>();
                cell = bridge.GetComponent<GridCell>();
                if (cell == null) cell = bridge.AddComponent<GridCell>();
                var node = EnsureNode(bridge.transform, "GridNode", Center(x, 0));
                cell.Configure(new Vector2Int(x, 0), true, false, false,
                    surface, node, surface, true);
            }
            else
            {
                var gateCondition = x == 12 ? gate.GetComponent<Collider>() : null;
                cell = EnsureTile(boardRoot.transform, x, 0, true, false, false,
                    gateCondition, false, playerStone, foundationStone);
            }
            cells.Add(cell);
            nodes.Add(cell.Node);
        }

        for (var x = 8; x <= 10; x++)
        {
            cells.Add(EnsureTile(boardRoot.transform, x, 2, false, true,
                x != 10, null, true, shadowStone, foundationStone));
        }

        board.Configure(Pitch, cells.ToArray());
        graph.Board = board;
        graph.Nodes = nodes.ToArray();
        graph.Edges = Array.Empty<PathEdge>();
        graph.Rebuild();

        // Keep the old authored route as an inactive, recoverable reference. It must
        // not contribute stray nodes after the generated square-cell graph takes over.
        foreach (Transform oldNode in graph.transform)
        {
            if (oldNode.GetComponent<PathNode>() != null) oldNode.gameObject.SetActive(false);
        }

        foreach (var name in new[]
        {
            "Stage01_T7_ApproachPath", "OffsetPath_Left", "OffsetPath_Right",
            "Stage01_T7_PostBridgePath", "Stage01_ShadowBranch"
        })
        {
            var oldFloor = Required(name);
            foreach (var collider in oldFloor.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var renderer in oldFloor.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        }

        Resize(gate, new Vector3(9f, 1.5f, 0f), new Vector3(0.55f, 3f, 1.65f));
        Resize(Required("Stage01_T7_RemoteShadowPlate"),
            new Vector3(6f, 1.12f, 3f), new Vector3(1.1f, 0.16f, 1.1f));
        Resize(Required("Stage01_T7_CheckpointAfterGate"),
            new Vector3(12f, 1.12f, 0f), new Vector3(1.2f, 0.24f, 1.4f));
        Resize(Required("Stage01_T7_Exit"),
            new Vector3(16.5f, 1f, 0f), new Vector3(0.35f, 2f, 1.5f));
        Resize(Required("Stage01_T7_CyanOutline_ShadowPlacement"),
            new Vector3(3f, 1.03f, 3f), new Vector3(1.3f, 0.03f, 1.3f));
        Resize(Required("Stage01_T7_CyanOutline_RemotePlate"),
            new Vector3(6f, 1.35f, 3f), new Vector3(1.3f, 0.03f, 1.3f));
        Resize(Required("Stage01_T7_CyanOutline_View"),
            new Vector3(0f, 0.9f, -0.8f), new Vector3(1.5f, 0.08f, 0.1f));

        var placement = player.GetComponent<ShadowPlacement>();
        var shadowController = player.GetComponent<ShadowController>();
        if (placement == null || shadowController == null)
        {
            throw new InvalidOperationException("Stage01 player needs shadow placement and controller.");
        }
        placement.Configure(board, shadowController);
        if (player.GetComponent<PathClickFeedback>() == null)
        {
            player.AddComponent<PathClickFeedback>();
        }
        var stageCamera = Required("Main Camera");
        var angleCamera = stageCamera.GetComponent<AngleSnapCamera>();
        var renderCamera = stageCamera.GetComponent<Camera>();
        if (angleCamera == null || renderCamera == null)
        {
            throw new InvalidOperationException("Stage01 camera needs angle snapping and a Camera component.");
        }
        angleCamera.ConfigureFollow(player.transform, 0.7f, new Vector2(-8f, 14f), 0.3f);
        renderCamera.fieldOfView = 45f;
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = new Color(0.046f, 0.075f, 0.092f);
        ConfigureTutorialHud();
        EditorUtility.SetDirty(board);
        EditorUtility.SetDirty(graph);
        EditorUtility.SetDirty(placement);
        EditorUtility.SetDirty(angleCamera);
        EditorUtility.SetDirty(renderCamera);
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Stage01 square grid, separate shadow lane, resized mechanisms, and graph saved.");
    }

    private static GridCell EnsureTile(Transform parent, int x, int z,
        bool player, bool shadow, bool spawn, Collider accessCondition,
        bool conditionEnabled, Material material, Material foundationMaterial)
    {
        var name = $"Tile_{x:D2}_{z:D2}";
        var existing = parent.Find(name);
        var root = existing != null ? existing.gameObject : new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = Center(x, z) - Vector3.up * 0.1f;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        root.layer = LayerMask.NameToLayer("Walkable");

        var collider = root.GetComponent<BoxCollider>();
        if (collider == null) collider = root.AddComponent<BoxCollider>();
        collider.enabled = true;
        collider.isTrigger = false;
        collider.center = Vector3.zero;
        collider.size = new Vector3(Pitch, 0.2f, Pitch);

        var visual = root.transform.Find("InsetStone");
        if (visual == null)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "InsetStone";
            cube.transform.SetParent(root.transform, false);
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<BoxCollider>());
            visual = cube.transform;
        }
        visual.localPosition = Vector3.zero;
        visual.localRotation = Quaternion.identity;
        visual.localScale = new Vector3(Pitch * 0.965f, 0.18f, Pitch * 0.965f);
        visual.GetComponent<Renderer>().sharedMaterial = material;

        var foundation = root.transform.Find("Foundation");
        if (foundation == null)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Foundation";
            cube.transform.SetParent(root.transform, false);
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<BoxCollider>());
            foundation = cube.transform;
        }
        foundation.localPosition = new Vector3(0f, -0.34f, 0f);
        foundation.localRotation = Quaternion.identity;
        foundation.localScale = new Vector3(Pitch * 0.83f, 0.5f, Pitch * 0.83f);
        foundation.GetComponent<Renderer>().sharedMaterial = foundationMaterial;

        PathNode node = null;
        if (player) node = EnsureNode(root.transform, "GridNode", Center(x, z));
        var cell = root.GetComponent<GridCell>();
        if (cell == null) cell = root.AddComponent<GridCell>();
        cell.Configure(new Vector2Int(x, z), player, shadow, spawn,
            collider, node, accessCondition, conditionEnabled);
        EditorUtility.SetDirty(cell);
        return cell;
    }

    private static PathNode EnsureNode(Transform parent, string name, Vector3 worldPosition)
    {
        var child = parent.Find(name);
        if (child == null)
        {
            var nodeObject = new GameObject(name);
            nodeObject.transform.SetParent(parent, false);
            child = nodeObject.transform;
        }
        child.position = worldPosition;
        var node = child.GetComponent<PathNode>();
        return node != null ? node : child.gameObject.AddComponent<PathNode>();
    }

    private static Vector3 Center(int x, int z) =>
        new Vector3(-9f + x * Pitch, FloorY, z * Pitch);

    private static void ConfigureTutorialHud()
    {
        var hud = Required("Stage01_T7_TutorialHUD");
        var backdrop = hud.transform.Find("TutorialBackdrop")?.GetComponent<RectTransform>();
        var message = hud.transform.Find("TutorialMessage")?.GetComponent<TMP_Text>();
        if (backdrop == null || message == null)
            throw new InvalidOperationException("Stage01 tutorial HUD is incomplete.");
        backdrop.anchoredPosition = new Vector2(0f, -24f);
        backdrop.sizeDelta = new Vector2(860f, 72f);
        var image = backdrop.GetComponent<Image>();
        var color = image.color;
        color.a = 0.62f;
        image.color = color;

        var textRect = message.GetComponent<RectTransform>();
        textRect.anchoredPosition = new Vector2(0f, -28f);
        textRect.sizeDelta = new Vector2(810f, 64f);
        message.fontSize = 24f;
        message.enableWordWrapping = true;
        EditorUtility.SetDirty(backdrop);
        EditorUtility.SetDirty(image);
        EditorUtility.SetDirty(textRect);
        EditorUtility.SetDirty(message);
    }

    private static GameObject Required(string name)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name) return child.gameObject;
            }
        }
        throw new InvalidOperationException($"Stage01 object '{name}' was not found.");
    }

    private static void Resize(GameObject target, Vector3 position, Vector3 scale)
    {
        target.transform.SetPositionAndRotation(position, Quaternion.identity);
        target.transform.localScale = scale;
        EditorUtility.SetDirty(target.transform);
    }
}
