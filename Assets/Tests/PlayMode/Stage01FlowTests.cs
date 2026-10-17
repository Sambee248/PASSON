using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class Stage01FlowTests
{
    [UnityTest]
    public IEnumerator Completion_can_advance_to_stage_two_but_not_before_the_exit()
    {
        var scene = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => scene.isDone);

        var hud = Object.FindFirstObjectByType<PuzzleHUD>();
        Assert.That(hud.TryAdvanceToNextStage(), Is.False);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Stage01_OffsetBridge"));

        var exit = GameObject.Find("Stage01_T7_Exit").GetComponent<StageExit>();
        var enter = typeof(StageExit).GetMethod("OnTriggerEnter", BindingFlags.Instance | BindingFlags.NonPublic);
        enter.Invoke(exit, new object[] { GameObject.Find("Stage01_T7_Player").GetComponent<CharacterController>() });
        yield return null;
        Assert.That(hud.TryAdvanceToNextStage(), Is.True);
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "Stage02_TurningPursuit");
        Assert.That(Object.FindFirstObjectByType<Stage02HUD>(), Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator Checkpoint_confirmation_returns_to_the_exit_objective()
    {
        var scene = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => scene.isDone);

        Object.FindFirstObjectByType<StageGate>().Open();
        var checkpoint = GameObject.Find("Stage01_T7_CheckpointAfterGate").GetComponent<CheckpointMarker>();
        Object.FindFirstObjectByType<CheckpointService>().Activate(checkpoint);
        var message = GameObject.Find("TutorialMessage").GetComponent<TMP_Text>();

        yield return new WaitForSeconds(0.3f);
        Assert.That(message.text, Is.EqualTo("检查点已记录"));
        yield return new WaitForSeconds(1.5f);
        Assert.That(message.text, Does.Contain("终点"),
            "The checkpoint toast must give the objective back to the player.");
    }

    [UnityTest]
    public IEnumerator Reaching_the_exit_fades_in_a_completion_card()
    {
        var scene = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => scene.isDone);

        var exit = GameObject.Find("Stage01_T7_Exit").GetComponent<StageExit>();
        var enter = typeof(StageExit).GetMethod("OnTriggerEnter", BindingFlags.Instance | BindingFlags.NonPublic);
        enter.Invoke(exit, new object[] { GameObject.Find("Stage01_T7_Player").GetComponent<CharacterController>() });
        yield return new WaitForSeconds(0.35f);

        var card = GameObject.Find("Stage01_CompletionPanel");
        Assert.That(card, Is.Not.Null, "Completion needs a visible end card.");
        Assert.That(card.GetComponent<CanvasGroup>().alpha, Is.GreaterThan(0.2f));
        Assert.That(GameObject.Find("TutorialMessage"), Is.Null,
            "The tutorial strip should not duplicate the completion title.");
        Assert.That(GameObject.Find("TutorialBackdrop"), Is.Null);
    }

    [UnityTest]
    public IEnumerator First_gate_rises_over_time_instead_of_teleporting()
    {
        var scene = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => scene.isDone);

        var gate = Object.FindFirstObjectByType<StageGate>();
        var closedY = gate.transform.localPosition.y;
        gate.Open();
        Assert.That(gate.IsOpen, Is.True);
        Assert.That(gate.GetComponent<Collider>().enabled, Is.False);
        Assert.That(gate.transform.localPosition.y, Is.LessThan(closedY + 2f),
            "The gate should visibly rise instead of jumping to its final height.");

        yield return new WaitForSeconds(0.2f);
        Assert.That(gate.transform.localPosition.y, Is.GreaterThan(closedY));
        yield return new WaitForSeconds(0.7f);
        Assert.That(gate.transform.localPosition.y, Is.EqualTo(closedY + 3f).Within(0.05f));
    }

    [UnityTest]
    public IEnumerator Player_and_shadow_use_visible_humanoid_silhouettes()
    {
        var scene = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => scene.isDone);

        var player = GameObject.Find("Stage01_T7_Player");
        AssertHumanoidVisual(player);
        Assert.That(player.GetComponent<ShadowController>().TrySpawn(new Vector3(-4f, 1f, 0f)), Is.True);
        yield return new WaitForSeconds(0.4f);
        AssertHumanoidVisual(GameObject.Find("Shadow(Clone)"));
    }

    private static void AssertHumanoidVisual(GameObject actor)
    {
        Assert.That(actor, Is.Not.Null);
        var oldRootMesh = actor.GetComponent<MeshRenderer>();
        if (oldRootMesh != null)
        {
            Assert.That(oldRootMesh.enabled, Is.False, "The old capsule must not cover the humanoid model.");
        }

        var oldShadowBody = actor.transform.Find("ShadowBody");
        if (oldShadowBody != null)
        {
            Assert.That(oldShadowBody.GetComponent<MeshRenderer>().enabled, Is.False);
        }

        var visual = actor.transform.Find("VisualRig");
        Assert.That(visual, Is.Not.Null, $"{actor.name} needs a humanoid visual rig.");
        foreach (var part in new[] { "Head", "Torso", "LeftArm", "RightArm", "LeftLeg", "RightLeg" })
        {
            var child = visual.Find(part);
            Assert.That(child, Is.Not.Null, $"{actor.name} is missing {part}.");
            Assert.That(child.GetComponent<Renderer>().enabled, Is.True);
            Assert.That(child.GetComponent<Collider>(), Is.Null,
                "Visual limbs must not change the actor collision or shadow placement rules.");
        }
    }

    [UnityTest]
    public IEnumerator Falling_shadow_is_recalled_without_resetting_the_player()
    {
        var scene = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => scene.isDone);

        var player = GameObject.Find("Stage01_T7_Player");
        var service = Object.FindFirstObjectByType<CheckpointService>();
        service.Activate(GameObject.Find("Stage01_T7_CheckpointAfterGate").GetComponent<CheckpointMarker>());
        Assert.That(service.ActiveMarker, Is.Not.Null);
        var controller = player.GetComponent<ShadowController>();
        Assert.That(controller.TrySpawn(new Vector3(-4f, 1f, 0f)), Is.True);
        yield return new WaitForSeconds(0.4f);
        Assert.That(GameObject.Find("Stage01_T7_Player"), Is.SameAs(player),
            "The scene unexpectedly reloaded before the deliberate shadow fall.");
        Assert.That(service.ActiveMarker, Is.Not.Null);

        var playerCapsule = player.GetComponent<CharacterController>();
        playerCapsule.enabled = false;
        player.transform.position = new Vector3(15f, 1f, 0f);
        playerCapsule.enabled = true;
        Physics.SyncTransforms();
        yield return new WaitForSeconds(0.1f);
        Assert.That(GameObject.Find("Stage01_T7_Player"), Is.SameAs(player),
            "The scene unexpectedly reloaded after moving the player to a walkway.");
        Assert.That(player.transform.position.x, Is.EqualTo(15f).Within(0.5f),
            "The player must remain on the walkway before the shadow falls.");
        var shadowFollower = controller.ActiveShadow;
        Assert.That(shadowFollower, Is.Not.Null);
        var shadowFell = false;
        shadowFollower.Fell += () => shadowFell = true;
        Assert.That(shadowFollower.enabled, Is.True, "The shadow must finish emerging before the fall test.");
        var shadowCapsule = shadowFollower.GetComponent<CharacterController>();
        shadowCapsule.enabled = false;
        shadowFollower.transform.position = new Vector3(-4f, -6f, 0f);
        shadowCapsule.enabled = true;
        Physics.SyncTransforms();
        yield return new WaitForSeconds(0.1f);

        Assert.That(player.transform.position.x, Is.EqualTo(15f).Within(0.5f),
            $"A shadow fall must not trigger the player checkpoint restore. marker={service.ActiveMarker}, scene={player.scene.name}");
        Assert.That(shadowFell, Is.True, "The shadow follower must report the fall.");
        Assert.That(controller.HasShadow, Is.False, "The fallen shadow should be recalled.");
    }

    [UnityTest]
    public IEnumerator Placement_highlight_does_not_block_shadow_placement()
    {
        var scene = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => scene.isDone);

        var player = GameObject.Find("Stage01_T7_Player");
        Object.FindFirstObjectByType<AngleSnapCamera>().SetAnchorIndexForTest(0);
        yield return new WaitForSeconds(0.25f);
        var path = player.GetComponent<PlayerPathController>();
        Assert.That(path.RestoreToNearestNode(new Vector3(0f, 1f, 0f), Quaternion.identity), Is.True);
        Assert.That(GameObject.Find("Stage01_T7_CyanOutline_ShadowPlacement").activeInHierarchy, Is.True);

        var board = Object.FindFirstObjectByType<GridBoard>();
        Assert.That(board.TryGetCell(new Vector2Int(8, 2), out var cell), Is.True);
        var placement = player.GetComponent<ShadowPlacement>();
        Assert.That(placement.TryPlace(cell), Is.True,
            "The placement cue must not block its own suggested shadow cell.");
    }

    [UnityTest]
    public IEnumerator First_stage_reaches_checkpoint_and_finish_after_solving_puzzle()
    {
        var scene = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => scene.isDone);

        var player = GameObject.Find("Stage01_T7_Player");
        var cameraSnap = Object.FindFirstObjectByType<AngleSnapCamera>();
        cameraSnap.SetAnchorIndexForTest(0);
        yield return new WaitForSeconds(0.25f);
        Assert.That(GameObject.Find("LockableBridge").GetComponent<Collider>().enabled, Is.True);

        var path = player.GetComponent<PlayerPathController>();
        var board = Object.FindFirstObjectByType<GridBoard>();
        Assert.That(path.RestoreToNearestNode(new Vector3(0f, 1f, 0f), Quaternion.identity), Is.True);
        Assert.That(board.TryGetCell(new Vector2Int(8, 2), out var shadowCell), Is.True);
        Assert.That(player.GetComponent<ShadowPlacement>().TryPlace(shadowCell), Is.True);
        var gate = Object.FindFirstObjectByType<StageGate>();
        yield return MovePlayerToPlate(path, board, gate);
        Assert.That(gate.IsOpen, Is.True);

        Assert.That(board.TryGetCell(new Vector2Int(14, 0), out var checkpointCell), Is.True);
        Assert.That(path.TryMoveTo(checkpointCell.Node), Is.True);
        var deadline = Time.time + 6f;
        while (path.IsMoving && Time.time < deadline) yield return null;
        Assert.That(path.IsMoving, Is.False);
        var checkpointService = Object.FindFirstObjectByType<CheckpointService>();
        Assert.That(checkpointService.ActiveMarker, Is.Not.Null,
            $"Player crossed checkpoint cell but no checkpoint activated; position={player.transform.position}");

        player.transform.position = new Vector3(13f, -6f, 0f);
        yield return new WaitForSeconds(0.1f);
        Assert.That(Vector3.Distance(player.transform.position, checkpointService.ActiveMarker.RespawnTransform.position),
            Is.LessThan(0.5f), "The checkpoint must recover a fall after the gate.");

        Assert.That(board.TryGetCell(new Vector2Int(17, 0), out var exitCell), Is.True);
        Assert.That(path.TryMoveTo(exitCell.Node), Is.True);
        deadline = Time.time + 4f;
        while (path.IsMoving && Time.time < deadline) yield return null;
        Assert.That(Object.FindFirstObjectByType<StageExit>().IsComplete, Is.True);
        Assert.That(Object.FindFirstObjectByType<PuzzleHUD>().CurrentTutorialStep,
            Is.EqualTo(Stage01TutorialStep.Finished));
    }

    [UnityTest]
    public IEnumerator Shadow_must_walk_to_the_remote_plate_to_open_the_gate()
    {
        var scene = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => scene.isDone);

        var player = GameObject.Find("Stage01_T7_Player");
        Object.FindFirstObjectByType<AngleSnapCamera>().SetAnchorIndexForTest(0);
        yield return new WaitForSeconds(0.25f);
        var path = player.GetComponent<PlayerPathController>();
        Assert.That(path.RestoreToNearestNode(new Vector3(0f, 1f, 0f), Quaternion.identity), Is.True);
        var placement = player.GetComponent<ShadowPlacement>();
        var board = Object.FindFirstObjectByType<GridBoard>();
        var plate = GameObject.Find("Stage01_T7_RemoteShadowPlate").GetComponent<PressurePlate>();
        var gate = Object.FindFirstObjectByType<StageGate>();
        Assert.That(gate.IsOpen, Is.False);
        Assert.That(board.TryGetCell(new Vector2Int(10, 2), out var plateCell), Is.True);
        Assert.That(plateCell.ShadowSpawnable, Is.False,
            "The plate is traversable by the shadow but cannot be directly targeted for placement.");
        Assert.That(placement.TryPlace(plateCell), Is.False);
        Assert.That(board.TryGetCell(new Vector2Int(8, 2), out var spawnCell), Is.True);
        Assert.That(placement.TryPlace(spawnCell), Is.True);
        yield return new WaitForSeconds(0.4f);
        Assert.That(plate.IsPressed, Is.False, "Spawning nearby must not press the plate.");
        Assert.That(gate.IsOpen, Is.False);
        yield return MovePlayerToPlate(path, board, gate);
        Assert.That(plate.IsPressed, Is.True, "The moved shadow must physically enter the plate trigger.");
        Assert.That(gate.IsOpen, Is.True);
    }

    private static IEnumerator MovePlayerToPlate(PlayerPathController path, GridBoard board, StageGate gate)
    {
        yield return new WaitForSeconds(0.4f);
        var shadowFollower = GameObject.Find("Stage01_T7_Player").GetComponent<ShadowController>().ActiveShadow;
        Assert.That(shadowFollower, Is.Not.Null, "A spawned shadow must follow player travel.");
        Assert.That(board.TryGetCell(new Vector2Int(8, 0), out var destination), Is.True);
        Assert.That(path.TryMoveTo(destination.Node), Is.True);
        var deadline = Time.time + 4f;
        while (Time.time < deadline && path.IsMoving) yield return null;
        var nearby = Physics.OverlapSphere(shadowFollower.transform.position + Vector3.up * 0.7f,
            1.3f, ~0, QueryTriggerInteraction.Ignore);
        Assert.That(gate.IsOpen, Is.True,
            $"The shadow did not reach the remote plate; last position {shadowFollower.transform.position}; nearby "
                + string.Join(", ", System.Array.ConvertAll(nearby, x => x.name + ":" + x.bounds)));
    }

    [UnityTest]
    public IEnumerator First_bridge_requires_the_player_to_select_the_valid_view()
    {
        var scene = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => scene.isDone);

        var cameraSnap = Object.FindFirstObjectByType<AngleSnapCamera>();
        var bridge = GameObject.Find("LockableBridge").GetComponent<Collider>();
        var hud = Object.FindFirstObjectByType<PuzzleHUD>();
        yield return new WaitForSeconds(0.35f);
        Assert.That(cameraSnap.IsLocked, Is.False, "The opening view must await an angle choice.");
        Assert.That(bridge.enabled, Is.False);
        Assert.That(hud.CurrentTutorialStep, Is.EqualTo(Stage01TutorialStep.AlignView));

        cameraSnap.SetAnchorIndexForTest(0);
        yield return new WaitForSeconds(0.25f);
        Assert.That(cameraSnap.IsLocked, Is.True);
        Assert.That(bridge.enabled, Is.True);
        Assert.That(hud.CurrentTutorialStep, Is.EqualTo(Stage01TutorialStep.PlaceShadow));
    }

    [UnityTest]
    public IEnumerator Only_the_player_can_complete_stage_one_at_the_exit()
    {
        var scene = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => scene.isDone);

        var exit = GameObject.Find("Stage01_T7_Exit").GetComponent<StageExit>();
        Assert.That(exit, Is.Not.Null);
        Assert.That(exit.GetComponent<Collider>().isTrigger, Is.True);

        var shadow = new GameObject("ExitTestShadow");
        shadow.layer = LayerMask.NameToLayer("Shadow");
        var shadowCollider = shadow.AddComponent<BoxCollider>();
        var enter = typeof(StageExit).GetMethod("OnTriggerEnter", BindingFlags.Instance | BindingFlags.NonPublic);
        try
        {
            enter.Invoke(exit, new object[] { shadowCollider });
            Assert.That(exit.IsComplete, Is.False);
            enter.Invoke(exit, new object[] { GameObject.Find("Stage01_T7_Player").GetComponent<CharacterController>() });
            Assert.That(exit.IsComplete, Is.True);
        }
        finally
        {
            Object.Destroy(shadow);
        }
    }

    [UnityTest]
    public IEnumerator Falling_before_the_checkpoint_restores_the_stage_start()
    {
        var scene = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => scene.isDone);

        var player = GameObject.Find("Stage01_T7_Player");
        var start = player.transform.position;
        Assert.That(Object.FindFirstObjectByType<CheckpointService>().ActiveMarker, Is.Null);

        player.transform.position = new Vector3(0f, -6f, 0f);
        yield return new WaitForSeconds(0.15f);
        var restartedPlayer = GameObject.Find("Stage01_T7_Player");
        Assert.That(restartedPlayer, Is.Not.Null);
        Assert.That(Vector3.Distance(restartedPlayer.transform.position, start), Is.LessThan(0.5f),
            "A fall before the first checkpoint must restart the first puzzle.");
    }

    [UnityTest]
    public IEnumerator Player_has_no_free_mover_and_stays_on_a_graph_node_when_idle()
    {
        var scene = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => scene.isDone);

        var player = GameObject.Find("Stage01_T7_Player");
        var controller = player.GetComponent<CharacterController>();
        Assert.That(controller.center.y, Is.EqualTo(controller.height * 0.5f).Within(0.01f),
            "The player capsule bottom must align with its transform on the walkway.");

        Assert.That(player.GetComponent<PathMover>(), Is.Null);
        Assert.That(player.GetComponent<PlayerPathController>(), Is.Not.Null);
        var start = player.transform.position;
        yield return new WaitForSeconds(0.3f);
        Assert.That(Vector3.Distance(player.transform.position, start), Is.LessThan(0.01f),
            "Idle player translation must not come from a free movement component.");
    }

    [UnityTest]
    public IEnumerator Stage_one_has_placement_surfaces_and_a_walkable_exit_route()
    {
        var scene = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => scene.isDone);

        var walkableMask = LayerMask.GetMask("Walkable");
        Assert.That(walkableMask, Is.Not.Zero, "The Walkable layer must exist.");
        foreach (var x in new[] { -9f, -3f, 3f, 12f, 16.5f })
        {
            Assert.That(
                Physics.Raycast(new Vector3(x, 4f, 0f), Vector3.down, 10f, walkableMask, QueryTriggerInteraction.Ignore),
                Is.True,
                $"Stage 1 needs walkable floor at x={x} for placement and the exit route.");
        }

        var placement = Object.FindFirstObjectByType<ShadowPlacement>();
        var controller = Object.FindFirstObjectByType<ShadowController>();
        Assert.That(placement, Is.Not.Null);
        Assert.That(controller, Is.Not.Null);
        Assert.That(Object.FindFirstObjectByType<PlayerPathController>()
            .RestoreToNearestNode(new Vector3(0f, 1f, 0f), Quaternion.identity), Is.True);
        var board = Object.FindFirstObjectByType<GridBoard>();
        Assert.That(board.TryGetCell(new Vector2Int(8, 2), out var spawnCell), Is.True);
        Assert.That(placement.TryPlace(spawnCell), Is.True,
            "F-placement must be possible on a highlighted shadow grid cell.");
        Assert.That(controller.HasShadow, Is.True);
    }

    [UnityTest]
    public IEnumerator Player_and_shadow_can_open_the_first_gate()
    {
        var scene = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => scene.isDone);

        var gate = Object.FindFirstObjectByType<StageGate>();
        Assert.That(gate, Is.Not.Null, "Stage 1 must contain the first brass gate.");

        var plate = GameObject.Find("Stage01_T7_RemoteShadowPlate").GetComponent<PressurePlate>();
        Assert.That(plate, Is.Not.Null);
        var player = new GameObject("PlateTestPlayer");
        var shadow = new GameObject("PlateTestShadow");
        player.layer = LayerMask.NameToLayer("Player");
        player.tag = "Player";
        shadow.layer = LayerMask.NameToLayer("Shadow");
        var playerCollider = player.AddComponent<BoxCollider>();
        var shadowCollider = shadow.AddComponent<BoxCollider>();
        var enter = typeof(PressurePlate).GetMethod("OnTriggerEnter", BindingFlags.Instance | BindingFlags.NonPublic);
        try
        {
            enter.Invoke(plate, new object[] { playerCollider });
            Assert.That(gate.IsOpen, Is.False, "Player must not open the shadow gate.");
            enter.Invoke(plate, new object[] { shadowCollider });
            Assert.That(gate.IsOpen, Is.True, "Shadow pressure must open the linked gate.");
        }
        finally
        {
            Object.Destroy(player);
            Object.Destroy(shadow);
        }
    }
}
