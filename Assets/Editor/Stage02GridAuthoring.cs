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

public static class Stage02GridAuthoring
{
    private const string ScenePath = "Assets/Scenes/Stage02_TurningPursuit.unity";
    private const float Pitch = 1.5f;
    private static readonly Vector3 Origin = new Vector3(-6f, 1f, 0f);

    [MenuItem("Tools/Stage02/Create Turning Pursuit Grid Stage")]
    public static void Create()
    {
        if (File.Exists(ScenePath))
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("Stage02 already exists; opened the saved scene without replacing authored changes.");
            return;
        }

        var playerStone = RequiredMaterial("Assets/Stage01_StoneGray.mat");
        var shadowStone = RequiredMaterial("Assets/Materials/ShadowInk.mat");
        var foundationStone = RequiredMaterial("Assets/Materials/Stage01Foundation.mat");
        var brass = RequiredMaterial("Assets/Materials/PlayerBrass.mat");
        var cyan = RequiredMaterial("Assets/Materials/ShadowCyan.mat");
        var danger = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Stage02Danger.mat");
        if (danger == null)
        {
            danger = new Material(Shader.Find("Standard"))
            {
                name = "Stage02Danger", color = new Color(0.67f, 0.12f, 0.13f)
            };
            AssetDatabase.CreateAsset(danger, "Assets/Materials/Stage02Danger.mat");
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var light = new GameObject("Directional Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

        var boardObject = new GameObject("Stage02_GridBoard");
        var board = boardObject.AddComponent<GridBoard>();
        var graphObject = new GameObject("Stage02_PathGraph");
        var graph = graphObject.AddComponent<PathGraph>();
        var cells = new List<GridCell>();
        var nodes = new List<PathNode>();

        for (var x = 0; x <= 3; x++)
            AddTile(boardObject.transform, cells, nodes, new Vector2Int(x, 0),
                Center(x, 0), true, false, false, null, playerStone, foundationStone);

        var rotor = new GameObject("Stage02_Turntable_Rotor").transform;
        rotor.position = Center(5, 0);
        var rotatingCells = new GridCell[3];
        for (var i = -1; i <= 1; i++)
            rotatingCells[i + 1] = AddTile(rotor, cells, nodes,
                new Vector2Int(5, i), rotor.position + Vector3.forward * (i * Pitch),
                true, false, false, null, brass, foundationStone);

        var gate = CreateGate("Stage02_ShadowGate", Center(11, 0), brass);
        var finalGate = CreateGate("Stage02_HazardGate", Center(13, 0), brass);
        for (var x = 7; x <= 14; x++)
        {
            var condition = x == 11 ? gate.GetComponent<Collider>()
                : x == 13 ? finalGate.GetComponent<Collider>() : null;
            AddTile(boardObject.transform, cells, nodes, new Vector2Int(x, 0),
                Center(x, 0), true, false, false, condition, playerStone, foundationStone);
        }
        for (var x = 8; x <= 10; x++)
            AddTile(boardObject.transform, cells, nodes, new Vector2Int(x, 2),
                Center(x, 2), false, true, x != 10, null, shadowStone, foundationStone);

        board.Configure(Pitch, cells.ToArray());
        graph.Board = board;
        graph.Nodes = nodes.ToArray();
        var beforeRotor = cells.First(cell => cell.Coordinate == new Vector2Int(3, 0));
        var afterRotor = cells.First(cell => cell.Coordinate == new Vector2Int(7, 0));
        graph.Edges = new[]
        {
            new PathEdge { Id = "turntable-west", From = beforeRotor.Node,
                To = rotatingCells[0].Node, Bidirectional = true, Enabled = true,
                MaxConnectionDistance = Pitch * 1.05f },
            new PathEdge { Id = "turntable-east", From = rotatingCells[2].Node,
                To = afterRotor.Node, Bidirectional = true, Enabled = true,
                MaxConnectionDistance = Pitch * 1.05f }
        };
        graph.Rebuild();

        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        if (playerPrefab == null) throw new InvalidOperationException("Player prefab is missing.");
        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
        player.name = "Stage02_Player";
        player.transform.position = Center(0, 0);
        var pathController = player.GetComponent<PlayerPathController>();
        var commands = player.GetComponent<PlayerCommandSource>();
        var shadowController = player.GetComponent<ShadowController>();
        var placement = player.GetComponent<ShadowPlacement>();
        if (pathController == null || commands == null || shadowController == null || placement == null)
            throw new InvalidOperationException("Player prefab is missing navigation or shadow components.");
        placement.Configure(board, shadowController);
        player.AddComponent<PathClickFeedback>();
        player.AddComponent<PlayerInteractor>().Configure(commands, pathController, 1.8f);

        var checkpoints = new GameObject("Stage02_CheckpointService").AddComponent<CheckpointService>();
        var checkpointObject = Cube("Stage02_Checkpoint", Center(8, 0) + Vector3.up * 0.08f,
            new Vector3(1.15f, 0.16f, 1.15f), cyan, true);
        var checkpointTrigger = checkpointObject.GetComponent<BoxCollider>();
        checkpointTrigger.center = new Vector3(0f, 4f, 0f);
        checkpointTrigger.size = new Vector3(1f, 9f, 1f);
        checkpointObject.AddComponent<CheckpointMarker>();
        var serviceData = new SerializedObject(checkpoints);
        serviceData.FindProperty("playerTransform").objectReferenceValue = player.transform;
        serviceData.FindProperty("shadowConfigurationSource").objectReferenceValue = shadowController;
        serviceData.ApplyModifiedPropertiesWithoutUndo();

        var console = Cube("Stage02_TurntableConsole", Center(3, 0)
            + new Vector3(0f, 0.5f, -1.45f), new Vector3(0.8f, 1f, 0.8f), brass, true);
        var module = console.AddComponent<RotationModule>();
        module.Configure(rotor, graph, new[] { 0f, 90f, 180f, 270f });

        var plateObject = Cube("Stage02_ShadowPlate", Center(10, 2) + Vector3.up * 0.09f,
            new Vector3(1.1f, 0.16f, 1.1f), cyan, true);
        ConfigurePlateTrigger(plateObject);
        var plate = plateObject.AddComponent<PressurePlate>();
        var plateData = new SerializedObject(plate);
        plateData.FindProperty("acceptedActors").intValue = (int)AcceptedActors.Shadow;
        plateData.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(plate.Activated, gate.GetComponent<StageGate>().Open);
        CreateDeviceMarker("Stage02_ShadowPlateLabel", plateObject.transform,
            Vector3.up * 1.25f, "影子踏板", new Color(0.05f, 0.27f, 0.32f, 0.94f),
            new Color(0.2f, 0.94f, 0.98f));

        var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemyObject.name = "Stage02_PatrolEnemy";
        enemyObject.transform.position = Center(12, 0) + Vector3.up * 0.8f;
        enemyObject.transform.localScale = new Vector3(0.62f, 0.8f, 0.62f);
        enemyObject.layer = LayerMask.NameToLayer("Enemy");
        enemyObject.GetComponent<Renderer>().sharedMaterial = danger;
        var enemy = enemyObject.AddComponent<PatrolEnemy>();
        var patrolA = new GameObject("Stage02_Patrol_A").transform;
        patrolA.position = Center(12, 0) + Vector3.up * 0.8f;
        var patrolB = new GameObject("Stage02_Patrol_B").transform;
        patrolB.position = Center(12, 0) + new Vector3(0.8f, 0.8f, 0f);
        enemy.Configure(new[] { patrolA, patrolB }, player.transform, checkpoints);

        var hazardConsole = Cube("Stage02_HazardConsole", Center(10, 0)
            + new Vector3(0f, 0.45f, -1.45f), new Vector3(0.8f, 0.9f, 0.8f), brass, true);
        var hazardVolume = Cube("Stage02_HazardZone", Center(12, 0)
            + Vector3.up * 0.8f, new Vector3(3.3f, 1.6f, 1.5f), danger, false);
        var volumeCollider = hazardVolume.AddComponent<BoxCollider>();
        volumeCollider.isTrigger = true;
        var warning = hazardVolume.GetComponent<Renderer>();
        warning.enabled = false;
        var hazard = hazardConsole.AddComponent<EnvironmentalHazard>();
        hazard.Configure(volumeCollider, warning, 0.6f, 1f);
        UnityEventTools.AddPersistentListener(hazard.Cleared, finalGate.GetComponent<StageGate>().Open);
        CreateDeviceMarker("Stage02_HazardLabel", hazardConsole.transform,
            Vector3.up * 1.35f, "陷阱机关 [E]", new Color(0.33f, 0.08f, 0.06f, 0.94f),
            new Color(1f, 0.4f, 0.2f));

        var exitObject = Cube("Stage02_Exit", Center(14, 0) + Vector3.up * 0.65f,
            new Vector3(0.55f, 1.3f, 1.2f), cyan, true);
        exitObject.GetComponent<BoxCollider>().isTrigger = true;
        var exit = exitObject.AddComponent<StageExit>();

        CreateCamera(player.transform);
        CreateHud(module, plate, gate.GetComponent<StageGate>(), hazard, shadowController, exit);

        Physics.SyncTransforms();
        graph.Rebuild();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Failed to save Stage02.");
        var buildScenes = EditorBuildSettings.scenes.ToList();
        if (buildScenes.All(entry => entry.path != ScenePath))
        {
            buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Stage02 turning pursuit grid scene created and added to build settings.");
    }

    [MenuItem("Tools/Stage02/Polish Existing Camera")]
    public static void PolishExistingCamera()
    {
        if (!File.Exists(ScenePath))
            throw new InvalidOperationException("Create Stage02 before polishing its camera.");
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var target = GameObject.Find("Stage02_CameraTarget")?.transform;
        var anchor = GameObject.Find("Stage02_CameraAnchor")?.transform;
        var camera = GameObject.Find("Main Camera")?.transform;
        if (target == null || anchor == null || camera == null)
            throw new InvalidOperationException("Stage02 camera rig is incomplete.");
        target.position = new Vector3(3f, 1f, 0f);
        anchor.position = new Vector3(3f, 14f, -17f);
        anchor.LookAt(target.position);
        camera.SetPositionAndRotation(anchor.position, anchor.rotation);
        var renderCamera = camera.GetComponent<Camera>();
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = new Color(0.04f, 0.066f, 0.083f);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Could not save polished Stage02 camera.");
        Debug.Log("Stage02 camera now exposes the turntable and both grid rows.");
    }

    [MenuItem("Tools/Stage02/Polish Existing Shadow Plate")]
    public static void PolishExistingShadowPlate()
    {
        if (!File.Exists(ScenePath))
            throw new InvalidOperationException("Create Stage02 before polishing its plate.");
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var plate = GameObject.Find("Stage02_ShadowPlate");
        if (plate == null) throw new InvalidOperationException("Stage02 shadow plate is missing.");
        ConfigurePlateTrigger(plate);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Could not save polished Stage02 plate.");
        Debug.Log("Stage02 shadow plate trigger now reaches the shadow capsule.");
    }

    [MenuItem("Tools/Stage02/Connect Existing Tutorial Gate")]
    public static void ConnectExistingTutorialGate()
    {
        if (!File.Exists(ScenePath))
            throw new InvalidOperationException("Create Stage02 before connecting its tutorial.");
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var hud = UnityEngine.Object.FindFirstObjectByType<Stage02HUD>();
        var label = GameObject.Find("GoalText")?.GetComponent<TMP_Text>();
        var module = UnityEngine.Object.FindFirstObjectByType<RotationModule>();
        var plate = UnityEngine.Object.FindFirstObjectByType<PressurePlate>();
        var gate = GameObject.Find("Stage02_ShadowGate")?.GetComponent<StageGate>();
        var hazard = UnityEngine.Object.FindFirstObjectByType<EnvironmentalHazard>();
        var shadow = GameObject.Find("Stage02_Player")?.GetComponent<ShadowController>();
        var exit = UnityEngine.Object.FindFirstObjectByType<StageExit>();
        if (hud == null || label == null || module == null || plate == null || gate == null
            || hazard == null || shadow == null || exit == null)
            throw new InvalidOperationException("Stage02 tutorial references are incomplete.");
        hud.Configure(label, module, plate, gate, hazard, shadow, exit);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Could not save Stage02 tutorial connection.");
        Debug.Log("Stage02 tutorial now follows the latched gate state.");
    }

    [MenuItem("Tools/Stage02/Label Existing Devices")]
    public static void LabelExistingDevices()
    {
        if (!File.Exists(ScenePath))
            throw new InvalidOperationException("Create Stage02 before labeling its devices.");
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var plate = GameObject.Find("Stage02_ShadowPlate");
        var console = GameObject.Find("Stage02_HazardConsole");
        if (plate == null || console == null)
            throw new InvalidOperationException("Stage02 devices are missing.");
        CreateDeviceMarker("Stage02_ShadowPlateLabel", plate.transform,
            Vector3.up * 1.25f, "影子踏板", new Color(0.05f, 0.27f, 0.32f, 0.94f),
            new Color(0.2f, 0.94f, 0.98f));
        CreateDeviceMarker("Stage02_HazardLabel", console.transform,
            Vector3.up * 1.35f, "陷阱机关 [E]", new Color(0.33f, 0.08f, 0.06f, 0.94f),
            new Color(1f, 0.4f, 0.2f));
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Could not save Stage02 device labels.");
        Debug.Log("Stage02 pressure plate and hazard console now have distinct world labels.");
    }

    private static void CreateDeviceMarker(string name, Transform anchor, Vector3 offset,
        string caption, Color background, Color accent)
    {
        var existing = GameObject.Find(name);
        if (existing != null)
        {
            existing.transform.localScale = Vector3.one * 0.013f;
            return;
        }
        var root = new GameObject(name, typeof(RectTransform));
        root.layer = LayerMask.NameToLayer("Ignore Raycast");
        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280f, 68f);
        rect.localScale = Vector3.one * 0.013f;
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var marker = root.AddComponent<WorldDeviceMarker>();
        marker.Configure(anchor, offset);

        var backdrop = new GameObject("Backdrop", typeof(RectTransform));
        backdrop.transform.SetParent(root.transform, false);
        var backdropRect = backdrop.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;
        var panel = backdrop.AddComponent<Image>();
        panel.color = background;
        panel.raycastTarget = false;

        var stripe = new GameObject("TypeStripe", typeof(RectTransform));
        stripe.transform.SetParent(root.transform, false);
        var stripeRect = stripe.GetComponent<RectTransform>();
        stripeRect.anchorMin = new Vector2(0f, 0f);
        stripeRect.anchorMax = new Vector2(0f, 1f);
        stripeRect.pivot = new Vector2(0f, 0.5f);
        stripeRect.anchoredPosition = Vector2.zero;
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
        if (text.font == null)
            throw new InvalidOperationException("The Stage01 CJK font is missing.");
        text.text = caption;
        text.fontSize = 32f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
    }

    private static GridCell AddTile(Transform parent, List<GridCell> cells,
        List<PathNode> nodes, Vector2Int coordinate, Vector3 center,
        bool playerWalkable, bool shadowWalkable, bool spawnable,
        Collider accessCondition, Material topMaterial, Material foundationMaterial)
    {
        var root = new GameObject($"Tile_{coordinate.x:D2}_{coordinate.y:+00;-00;00}");
        root.transform.SetParent(parent, true);
        root.transform.position = center - Vector3.up * 0.1f;
        root.layer = LayerMask.NameToLayer("Walkable");
        var surface = root.AddComponent<BoxCollider>();
        surface.size = new Vector3(Pitch, 0.2f, Pitch);
        CubeChild(root.transform, "InsetStone", Vector3.zero,
            new Vector3(Pitch * 0.965f, 0.18f, Pitch * 0.965f), topMaterial);
        CubeChild(root.transform, "Foundation", new Vector3(0f, -0.34f, 0f),
            new Vector3(Pitch * 0.83f, 0.5f, Pitch * 0.83f), foundationMaterial);
        PathNode node = null;
        if (playerWalkable)
        {
            var nodeObject = new GameObject("GridNode");
            nodeObject.transform.SetParent(root.transform, false);
            nodeObject.transform.localPosition = Vector3.up * 0.1f;
            node = nodeObject.AddComponent<PathNode>();
            nodes.Add(node);
        }
        var cell = root.AddComponent<GridCell>();
        cell.Configure(coordinate, playerWalkable, shadowWalkable, spawnable,
            surface, node, accessCondition, false);
        cells.Add(cell);
        return cell;
    }

    private static GameObject CreateGate(string name, Vector3 center, Material material)
    {
        var root = Cube(name, center + Vector3.up * 0.6f,
            new Vector3(0.56f, 2.4f, 1.55f), material, true);
        root.AddComponent<StageGate>();
        return root;
    }

    private static void ConfigurePlateTrigger(GameObject plate)
    {
        var trigger = plate.GetComponent<BoxCollider>();
        if (trigger == null) throw new InvalidOperationException("Stage02 plate has no collider.");
        trigger.isTrigger = true;
        // Keep the rendered pad slim while the trigger reaches the shadow capsule.
        trigger.center = new Vector3(0f, 2.5f, 0f);
        trigger.size = new Vector3(1f, 6f, 1f);
        var oldBody = plate.GetComponent<Rigidbody>();
        if (oldBody != null) UnityEngine.Object.DestroyImmediate(oldBody);
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

    private static void CubeChild(Transform parent, string name, Vector3 localPosition,
        Vector3 scale, Material material)
    {
        var child = Cube(name, Vector3.zero, scale, material, false);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
    }

    private static void CreateCamera(Transform player)
    {
        var target = new GameObject("Stage02_CameraTarget").transform;
        target.position = new Vector3(3f, 1f, 0f);
        var anchor = new GameObject("Stage02_CameraAnchor").transform;
        anchor.position = new Vector3(3f, 14f, -17f);
        anchor.LookAt(target.position);
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
        var camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 45f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.04f, 0.066f, 0.083f);
        cameraObject.AddComponent<AudioListener>();
        var cameraRig = cameraObject.AddComponent<AngleSnapCamera>();
        cameraRig.ConfigureScene(target, anchor);
        cameraRig.ConfigureFollow(player, 0.6f, new Vector2(-6f, 15f), 0.35f);
    }

    private static void CreateHud(RotationModule module, PressurePlate plate, StageGate gate,
        EnvironmentalHazard hazard, ShadowController shadow, StageExit exit)
    {
        var canvasObject = new GameObject("Stage02_HUD", typeof(RectTransform));
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();
        var backdrop = new GameObject("GoalBackdrop", typeof(RectTransform));
        backdrop.transform.SetParent(canvasObject.transform, false);
        var backgroundRect = backdrop.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0.5f, 1f);
        backgroundRect.anchorMax = new Vector2(0.5f, 1f);
        backgroundRect.pivot = new Vector2(0.5f, 1f);
        backgroundRect.anchoredPosition = new Vector2(0f, -24f);
        backgroundRect.sizeDelta = new Vector2(880f, 72f);
        var image = backdrop.AddComponent<Image>();
        image.color = new Color(0.035f, 0.07f, 0.1f, 0.64f);
        image.raycastTarget = false;
        var textObject = new GameObject("GoalText", typeof(RectTransform));
        textObject.transform.SetParent(canvasObject.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 1f);
        textRect.anchorMax = new Vector2(0.5f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = new Vector2(0f, -28f);
        textRect.sizeDelta = new Vector2(820f, 64f);
        var label = textObject.AddComponent<TextMeshProUGUI>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/Stage01TutorialCjk Dynamic.asset");
        label.fontSize = 24f;
        label.color = new Color(0.9f, 0.95f, 0.94f);
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.raycastTarget = false;
        label.text = "靠近黄铜转盘按 E，让断开的格子接上";
        canvasObject.AddComponent<Stage02HUD>().Configure(label, module, plate, gate,
            hazard, shadow, exit);
    }

    private static Vector3 Center(int x, int z) => Origin + new Vector3(x * Pitch, 0f, z * Pitch);

    private static Material RequiredMaterial(string path) =>
        AssetDatabase.LoadAssetAtPath<Material>(path) ??
        throw new InvalidOperationException($"Required material is missing: {path}");
}
