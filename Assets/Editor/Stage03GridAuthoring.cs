using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Stage03GridAuthoring
{
    public const string ScenePath = "Assets/Scenes/Stage03_SharedShadowCourtyards.unity";
    private const float Pitch = 1.5f;
    private static readonly Vector3 Origin = new Vector3(-15f, 1f, 9f);

    [MenuItem("Tools/Stage03/Create Shared Shadow Courtyards")]
    public static void Create()
    {
        if (File.Exists(ScenePath))
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("Stage03 already exists; opened the saved scene without replacing edits.");
            return;
        }

        var playerStone = RequireMaterial("Assets/Stage01_StoneGray.mat");
        var shadowStone = RequireMaterial("Assets/Materials/ShadowInk.mat");
        var foundation = RequireMaterial("Assets/Materials/Stage01Foundation.mat");
        var brass = RequireMaterial("Assets/Materials/PlayerBrass.mat");
        var cyan = RequireMaterial("Assets/Materials/ShadowCyan.mat");
        var danger = RequireMaterial("Assets/Materials/Stage02Danger.mat");
        var sharedStone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Stage03SharedStone.mat");
        if (sharedStone == null)
        {
            var shader = Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Standard shader is missing.");
            sharedStone = new Material(shader)
            {
                name = "Stage03SharedStone",
                color = new Color(0.25f, 0.41f, 0.44f)
            };
            AssetDatabase.CreateAsset(sharedStone, "Assets/Materials/Stage03SharedStone.mat");
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var sun = new GameObject("Directional Light").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.25f;
        sun.transform.rotation = Quaternion.Euler(48f, -36f, 0f);

        var boardObject = new GameObject("Stage03_GridBoard");
        var board = boardObject.AddComponent<GridBoard>();
        var graphObject = new GameObject("Stage03_PathGraph");
        var graph = graphObject.AddComponent<PathGraph>();
        var cells = new List<GridCell>();
        var nodes = new List<PathNode>();
        var indexed = new Dictionary<Vector2Int, GridCell>();

        var gateA = CreateGate("Stage03_GateA", Center(8, 6), brass);
        var gateB = CreateGate("Stage03_GateB", Center(17, 11), brass);
        var gateACollider = gateA.GetComponent<Collider>();
        var gateBCollider = gateB.GetComponent<Collider>();

        for (var y = 5; y <= 9; y++)
            for (var x = 1; x <= 7; x++)
                AddTile(boardObject.transform, cells, nodes, indexed, x, y,
                    true, true, x == 3 && y == 6, null,
                    x == 3 && y == 6 ? cyan : sharedStone, foundation);
        for (var y = 3; y <= 4; y++)
            for (var x = 5; x <= 7; x++)
                AddTile(boardObject.transform, cells, nodes, indexed, x, y,
                    false, true, false, null, shadowStone, foundation);
        AddTile(boardObject.transform, cells, nodes, indexed, 8, 6,
            true, false, false, gateACollider, playerStone, foundation);
        AddTile(boardObject.transform, cells, nodes, indexed, 9, 6,
            true, false, false, null, playerStone, foundation);

        var rotor = new GameObject("Stage03_Rotor").transform;
        rotor.position = Center(11, 6);
        for (var x = 10; x <= 12; x++)
            AddTile(rotor, cells, nodes, indexed, x, 6,
                true, false, false, null, brass, foundation);

        AddTile(boardObject.transform, cells, nodes, indexed, 13, 6,
            true, false, false, null, playerStone, foundation);
        for (var y = 1; y <= 4; y++)
            AddTile(boardObject.transform, cells, nodes, indexed, 11, y,
                true, false, false, null, playerStone, foundation);
        for (var y = 8; y <= 11; y++)
            AddTile(boardObject.transform, cells, nodes, indexed, 11, y,
                true, false, false, null, playerStone, foundation);

        for (var y = 4; y <= 9; y++)
            for (var x = 14; x <= 19; x++)
            {
                var material = x == 15 && y == 6 ? cyan
                    : y == 9 && x >= 16 && x <= 17 ? danger : sharedStone;
                AddTile(boardObject.transform, cells, nodes, indexed, x, y,
                    true, true, x == 15 && y == 6, null, material, foundation);
            }
        for (var y = 2; y <= 3; y++)
            for (var x = 17; x <= 19; x++)
                AddTile(boardObject.transform, cells, nodes, indexed, x, y,
                    false, true, false, null, shadowStone, foundation);

        AddTile(boardObject.transform, cells, nodes, indexed, 18, 10,
            true, false, false, null, playerStone, foundation);
        for (var x = 12; x <= 18; x++)
            AddTile(boardObject.transform, cells, nodes, indexed, x, 11,
                true, false, false, x == 17 ? gateBCollider : null,
                playerStone, foundation);

        board.Configure(Pitch, cells.ToArray());
        graph.Board = board;
        graph.Nodes = nodes.ToArray();
        graph.Edges = new[]
        {
            Connector("west", Node(9, 6), Node(10, 6)),
            Connector("east", Node(12, 6), Node(13, 6)),
            Connector("north", Node(11, 4), Node(10, 6)),
            Connector("south", Node(12, 6), Node(11, 8))
        };
        graph.Rebuild();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        if (prefab == null) throw new InvalidOperationException("Player prefab is missing.");
        var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        player.name = "Stage03_Player";
        player.transform.position = Center(2, 8);
        var pathController = RequireComponent<PlayerPathController>(player);
        var commands = RequireComponent<PlayerCommandSource>(player);
        var shadowController = RequireComponent<ShadowController>(player);
        RequireComponent<ShadowPlacement>(player).Configure(board, shadowController);
        if (player.GetComponent<PathClickFeedback>() == null) player.AddComponent<PathClickFeedback>();
        player.AddComponent<PlayerInteractor>().Configure(commands, pathController, 1.8f);

        var checkpointService = new GameObject("Stage03_CheckpointService")
            .AddComponent<CheckpointService>();
        var serviceData = new SerializedObject(checkpointService);
        serviceData.FindProperty("playerTransform").objectReferenceValue = player.transform;
        serviceData.FindProperty("shadowConfigurationSource").objectReferenceValue = shadowController;
        serviceData.ApplyModifiedPropertiesWithoutUndo();
        var checkpoint = Cube("Stage03_Checkpoint", Center(14, 6) + Vector3.up * 0.08f,
            new Vector3(1.15f, 0.16f, 1.15f), cyan, true);
        var checkpointTrigger = checkpoint.GetComponent<BoxCollider>();
        checkpointTrigger.isTrigger = true;
        checkpointTrigger.center = new Vector3(0f, 4f, 0f);
        checkpointTrigger.size = new Vector3(1f, 9f, 1f);
        checkpoint.AddComponent<CheckpointMarker>();

        var plateA = CreatePlate("Stage03_PlateA", Center(6, 3), cyan);
        var plateB = CreatePlate("Stage03_PlateB", Center(18, 2), cyan);
        UnityEventTools.AddPersistentListener(plateA.Activated, gateA.GetComponent<StageGate>().Open);
        UnityEventTools.AddPersistentListener(plateB.Activated, gateB.GetComponent<StageGate>().Open);
        CreateMarker("Stage03_PlateALabel", plateA.transform, "影子踏板 ①", cyan.color);
        CreateMarker("Stage03_PlateBLabel", plateB.transform, "影子踏板 ②", cyan.color);

        var rotorConsole = Cube("Stage03_RotorConsole",
            Center(11, 9) + new Vector3(1.0f, 0.45f, 0f),
            new Vector3(0.55f, 0.9f, 0.55f), brass, true);
        rotorConsole.AddComponent<RotationModule>().Configure(rotor, graph, new[] { 0f, 90f });
        CreateMarker("Stage03_RotorLabel", rotorConsole.transform, "旋桥控制台 [E]", brass.color);

        var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemyObject.name = "Stage03_PatrolEnemy";
        enemyObject.transform.position = Center(16, 9) + Vector3.up * 0.8f;
        enemyObject.transform.localScale = new Vector3(0.62f, 0.8f, 0.62f);
        enemyObject.layer = LayerMask.NameToLayer("Enemy");
        enemyObject.GetComponent<Renderer>().sharedMaterial = danger;
        var patrolA = new GameObject("Stage03_PatrolA").transform;
        patrolA.position = Center(16, 9) + Vector3.up * 0.8f;
        var patrolB = new GameObject("Stage03_PatrolB").transform;
        patrolB.position = Center(17, 9) + Vector3.up * 0.8f;
        var enemy = enemyObject.AddComponent<PatrolEnemy>();
        enemy.Configure(new[] { patrolA, patrolB }, player.transform, checkpointService);
        var enemyData = new SerializedObject(enemy);
        enemyData.FindProperty("detectionRange").floatValue = 1.2f;
        enemyData.ApplyModifiedPropertiesWithoutUndo();

        var trapConsole = Cube("Stage03_TrapConsole",
            Center(19, 8) + new Vector3(1.45f, 0.45f, 0f),
            new Vector3(0.7f, 0.9f, 0.7f), danger, true);
        var trapVolume = Cube("Stage03_TrapVolume",
            (Center(16, 9) + Center(17, 9)) * 0.5f + Vector3.up * 0.8f,
            new Vector3(3.3f, 1.6f, 1.5f), danger, false);
        var volumeCollider = trapVolume.AddComponent<BoxCollider>();
        volumeCollider.isTrigger = true;
        var warning = trapVolume.GetComponent<Renderer>();
        warning.enabled = false;
        trapConsole.AddComponent<EnvironmentalHazard>().Configure(volumeCollider, warning, 0.6f, 1f);
        CreateMarker("Stage03_TrapLabel", trapConsole.transform, "陷阱机关 [E]", danger.color);

        var exitObject = Cube("Stage03_Exit", Center(11, 1) + Vector3.up * 0.65f,
            new Vector3(1.2f, 1.3f, 0.55f), cyan, true);
        exitObject.GetComponent<BoxCollider>().isTrigger = true;
        var exit = exitObject.AddComponent<StageExit>();
        CreateMarker("Stage03_ExitLabel", exitObject.transform, "北侧出口", cyan.color);

        CreateHud(plateA, plateB, gateA.GetComponent<StageGate>(),
            gateB.GetComponent<StageGate>(), rotorConsole.GetComponent<RotationModule>(),
            shadowController, exit);

        CreateArchitecture(sharedStone, brass, foundation);
        CreateCamera(player.transform);
        Physics.SyncTransforms();
        graph.Rebuild();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Failed to save Stage03.");
        var buildScenes = EditorBuildSettings.scenes.ToList();
        if (buildScenes.All(entry => entry.path != ScenePath))
        {
            buildScenes.Insert(Math.Min(3, buildScenes.Count),
                new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Stage03 shared-shadow courtyards created and added to Build Settings.");

        PathNode Node(int x, int y) => indexed[new Vector2Int(x, y)].Node;
    }

    [MenuItem("Tools/Stage03/Wire Shared Shadow HUD")]
    public static void WireExistingHud()
    {
        if (!File.Exists(ScenePath))
            throw new InvalidOperationException("Create Stage03 before wiring its HUD.");
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var plateA = GameObject.Find("Stage03_PlateA")?.GetComponent<PressurePlate>();
        var plateB = GameObject.Find("Stage03_PlateB")?.GetComponent<PressurePlate>();
        var gateA = GameObject.Find("Stage03_GateA")?.GetComponent<StageGate>();
        var gateB = GameObject.Find("Stage03_GateB")?.GetComponent<StageGate>();
        var rotor = GameObject.Find("Stage03_RotorConsole")?.GetComponent<RotationModule>();
        var shadow = GameObject.Find("Stage03_Player")?.GetComponent<ShadowController>();
        var exit = GameObject.Find("Stage03_Exit")?.GetComponent<StageExit>();
        if (plateA == null || plateB == null || gateA == null || gateB == null
            || rotor == null || shadow == null || exit == null)
            throw new InvalidOperationException("Stage03 HUD references are incomplete.");
        CreateHud(plateA, plateB, gateA, gateB, rotor, shadow, exit);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Could not save Stage03 HUD.");
        Debug.Log("Stage03 shared-shadow HUD wired.");
    }

    [MenuItem("Tools/Stage03/Label Existing Exit")]
    public static void LabelExistingExit()
    {
        if (!File.Exists(ScenePath))
            throw new InvalidOperationException("Create Stage03 before labeling its exit.");
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (GameObject.Find("Stage03_ExitLabel") == null)
        {
            var exit = GameObject.Find("Stage03_Exit");
            if (exit == null) throw new InvalidOperationException("Stage03 exit is missing.");
            var cyan = RequireMaterial("Assets/Materials/ShadowCyan.mat");
            CreateMarker("Stage03_ExitLabel", exit.transform, "北侧出口", cyan.color);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Could not save Stage03 exit label.");
        }
    }

    private static void CreateHud(PressurePlate plateA, PressurePlate plateB,
        StageGate gateA, StageGate gateB, RotationModule rotor,
        ShadowController shadow, StageExit exit)
    {
        var existing = UnityEngine.Object.FindFirstObjectByType<Stage03HUD>();
        if (existing != null)
        {
            var currentLabel = GameObject.Find("Stage03GoalText")?.GetComponent<TMP_Text>();
            if (currentLabel == null)
                throw new InvalidOperationException("Stage03 HUD has no goal label.");
            existing.Configure(currentLabel, plateA, plateB, gateA, gateB, rotor, shadow, exit);
            return;
        }

        var canvasObject = new GameObject("Stage03_HUD", typeof(RectTransform));
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        var backdrop = new GameObject("Stage03GoalBackdrop", typeof(RectTransform));
        backdrop.transform.SetParent(canvasObject.transform, false);
        var backgroundRect = backdrop.GetComponent<RectTransform>();
        backgroundRect.anchorMin = backgroundRect.anchorMax = new Vector2(0.5f, 1f);
        backgroundRect.pivot = new Vector2(0.5f, 1f);
        backgroundRect.anchoredPosition = new Vector2(0f, -24f);
        backgroundRect.sizeDelta = new Vector2(1040f, 76f);
        var image = backdrop.AddComponent<Image>();
        image.color = new Color(0.035f, 0.07f, 0.1f, 0.76f);
        image.raycastTarget = false;

        var textObject = new GameObject("Stage03GoalText", typeof(RectTransform));
        textObject.transform.SetParent(canvasObject.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = new Vector2(0f, -28f);
        textRect.sizeDelta = new Vector2(990f, 68f);
        var label = textObject.AddComponent<TextMeshProUGUI>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/Stage01TutorialCjk Dynamic.asset");
        if (label.font == null) throw new InvalidOperationException("Stage03 HUD CJK font is missing.");
        label.fontSize = 27f;
        label.color = new Color(0.9f, 0.96f, 0.95f);
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.raycastTarget = false;
        label.text = "按住 F 查看可投放点，松开 F 将影子放到西庭的青色格子";
        canvasObject.AddComponent<Stage03HUD>().Configure(label, plateA, plateB,
            gateA, gateB, rotor, shadow, exit);
    }

    private static PathEdge Connector(string name, PathNode from, PathNode to) =>
        new PathEdge
        {
            Id = "stage03-" + name, From = from, To = to,
            Bidirectional = true, Enabled = true,
            MaxConnectionDistance = Pitch * 1.05f
        };

    private static GridCell AddTile(Transform parent, List<GridCell> cells,
        List<PathNode> nodes, Dictionary<Vector2Int, GridCell> indexed,
        int x, int y, bool player, bool shadow, bool spawn,
        Collider condition, Material top, Material foundation)
    {
        var coordinate = new Vector2Int(x, y);
        if (indexed.ContainsKey(coordinate))
            throw new InvalidOperationException($"Duplicate Stage03 cell {coordinate}.");
        var root = new GameObject($"Tile_{x:D2}_{y:D2}");
        root.transform.SetParent(parent, true);
        root.transform.position = Center(x, y) - Vector3.up * 0.1f;
        root.layer = LayerMask.NameToLayer("Walkable");
        var surface = root.AddComponent<BoxCollider>();
        surface.size = new Vector3(Pitch, 0.2f, Pitch);
        CubeChild(root.transform, "InsetStone", Vector3.zero,
            new Vector3(Pitch * 0.965f, 0.18f, Pitch * 0.965f), top);
        CubeChild(root.transform, "Foundation", new Vector3(0f, -0.34f, 0f),
            new Vector3(Pitch * 0.83f, 0.5f, Pitch * 0.83f), foundation);
        PathNode node = null;
        if (player)
        {
            var nodeObject = new GameObject("GridNode");
            nodeObject.transform.SetParent(root.transform, false);
            nodeObject.transform.localPosition = Vector3.up * 0.1f;
            node = nodeObject.AddComponent<PathNode>();
            nodes.Add(node);
        }
        var cell = root.AddComponent<GridCell>();
        cell.Configure(coordinate, player, shadow, spawn, surface, node, condition, false);
        cells.Add(cell);
        indexed.Add(coordinate, cell);
        return cell;
    }

    private static GameObject CreateGate(string name, Vector3 center, Material material)
    {
        var gate = Cube(name, center + Vector3.up * 0.6f,
            new Vector3(1.5f, 2.4f, 0.56f), material, true);
        gate.AddComponent<StageGate>();
        return gate;
    }

    private static PressurePlate CreatePlate(string name, Vector3 center, Material material)
    {
        var plateObject = Cube(name, center + Vector3.up * 0.09f,
            new Vector3(1.1f, 0.16f, 1.1f), material, true);
        var trigger = plateObject.GetComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 2.5f, 0f);
        trigger.size = new Vector3(1f, 6f, 1f);
        var plate = plateObject.AddComponent<PressurePlate>();
        var data = new SerializedObject(plate);
        data.FindProperty("acceptedActors").intValue = (int)AcceptedActors.Shadow;
        data.ApplyModifiedPropertiesWithoutUndo();
        return plate;
    }

    private static void CreateMarker(string name, Transform anchor, string caption, Color accent)
    {
        var root = new GameObject(name, typeof(RectTransform));
        root.layer = LayerMask.NameToLayer("Ignore Raycast");
        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300f, 68f);
        rect.localScale = Vector3.one * 0.018f;
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        root.AddComponent<WorldDeviceMarker>().Configure(anchor, Vector3.up * 1.45f);
        var background = new GameObject("Backdrop", typeof(RectTransform));
        background.transform.SetParent(root.transform, false);
        var backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        var image = background.AddComponent<Image>();
        image.color = new Color(0.035f, 0.075f, 0.09f, 0.92f);
        image.raycastTarget = false;
        var stripe = new GameObject("TypeStripe", typeof(RectTransform));
        stripe.transform.SetParent(root.transform, false);
        var stripeRect = stripe.GetComponent<RectTransform>();
        stripeRect.anchorMin = new Vector2(0f, 0f);
        stripeRect.anchorMax = new Vector2(0f, 1f);
        stripeRect.pivot = new Vector2(0f, 0.5f);
        stripeRect.sizeDelta = new Vector2(13f, 0f);
        var stripeImage = stripe.AddComponent<Image>();
        stripeImage.color = accent;
        stripeImage.raycastTarget = false;
        var textObject = new GameObject("Caption", typeof(RectTransform));
        textObject.transform.SetParent(root.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 4f);
        textRect.offsetMax = new Vector2(-6f, -4f);
        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/Stage01TutorialCjk Dynamic.asset");
        if (text.font == null) throw new InvalidOperationException("CJK world-label font is missing.");
        text.text = caption;
        text.fontSize = 30f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
    }

    private static void CreateArchitecture(Material shared, Material brass, Material foundation)
    {
        var root = new GameObject("Stage03_Architecture").transform;
        foreach (var coordinate in new[]
        {
            new Vector2Int(0, 4), new Vector2Int(8, 4),
            new Vector2Int(13, 3), new Vector2Int(20, 3),
            new Vector2Int(0, 10), new Vector2Int(20, 10)
        })
        {
            var pillar = Cube($"Pillar_{coordinate.x}_{coordinate.y}",
                Center(coordinate.x, coordinate.y) + Vector3.up * 1.2f,
                new Vector3(0.9f, 3.4f, 0.9f), shared, false);
            pillar.transform.SetParent(root, true);
            var cap = Cube($"PillarCap_{coordinate.x}_{coordinate.y}",
                pillar.transform.position + Vector3.up * 1.82f,
                new Vector3(1.2f, 0.22f, 1.2f), brass, false);
            cap.transform.SetParent(root, true);
        }
        var rotorBase = Cube("RotorBase", Center(11, 6) - Vector3.up * 0.78f,
            new Vector3(5.3f, 0.55f, 5.3f), foundation, false);
        rotorBase.transform.SetParent(root, true);
    }

    private static void CreateCamera(Transform player)
    {
        var target = new GameObject("Stage03_CameraTarget").transform;
        target.position = new Vector3(0f, 1f, 0f);
        var anchor = new GameObject("Stage03_CameraAnchor").transform;
        anchor.position = new Vector3(0f, 28f, -20f);
        anchor.LookAt(target.position);
        var alternate = new GameObject("Stage03_CameraAlternateAnchor").transform;
        alternate.position = new Vector3(0f, 28f, 20f);
        alternate.LookAt(target.position);
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
        var camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 45f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.067f, 0.08f);
        cameraObject.AddComponent<AudioListener>();
        var rig = cameraObject.AddComponent<AngleSnapCamera>();
        rig.ConfigureScene(target, anchor, alternate);
        rig.ConfigureFollow(player, 0.25f, new Vector2(-15f, 15f), 0.4f);
    }

    private static GameObject Cube(string name, Vector3 position, Vector3 scale,
        Material material, bool collider)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.position = position;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        if (!collider) UnityEngine.Object.DestroyImmediate(cube.GetComponent<BoxCollider>());
        return cube;
    }

    private static void CubeChild(Transform parent, string name,
        Vector3 localPosition, Vector3 scale, Material material)
    {
        var child = Cube(name, Vector3.zero, scale, material, false);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
    }

    private static T RequireComponent<T>(GameObject owner) where T : Component =>
        owner.GetComponent<T>() ?? throw new InvalidOperationException(
            $"{owner.name} requires {typeof(T).Name}.");

    private static Material RequireMaterial(string path) =>
        AssetDatabase.LoadAssetAtPath<Material>(path) ??
        throw new InvalidOperationException($"Required material is missing: {path}");

    private static Vector3 Center(int x, int y) =>
        Origin + new Vector3(x * Pitch, 0f, -y * Pitch);
}
