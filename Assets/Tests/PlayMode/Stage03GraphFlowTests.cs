using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TMPro;

public sealed class Stage03GraphFlowTests
{
    private const string SceneName = "Stage03_SharedShadowCourtyards";

    [UnityTest]
    public IEnumerator Hud_guides_the_shared_shadow_objective()
    {
        yield return SceneManager.LoadSceneAsync(SceneName);
        yield return null;
        var label = GameObject.Find("Stage03GoalText")?.GetComponent<TMP_Text>();
        Assert.That(label, Is.Not.Null);
        Assert.That(label.text, Does.Contain("影子"));
        GameObject.Find("Stage03_PlateA").GetComponent<PressurePlate>().SetPressedForTest(true);
        yield return null;
        Assert.That(label.text, Does.Contain("第二"));
        GameObject.Find("Stage03_PlateB").GetComponent<PressurePlate>().SetPressedForTest(true);
        yield return null;
        Assert.That(label.text, Does.Contain("陷阱"));
        Assert.That(label.text, Does.Contain("南廊"));
        GameObject.Find("Stage03_RotorConsole").GetComponent<RotationModule>().RotateNext();
        yield return null;
        Assert.That(label.text, Does.Contain("北侧出口"));
    }

    [UnityTest]
    public IEnumerator Angle_keys_have_a_distinct_second_view_that_keeps_the_player_visible()
    {
        yield return SceneManager.LoadSceneAsync(SceneName);
        yield return null;
        var alternate = GameObject.Find("Stage03_CameraAlternateAnchor");
        Assert.That(alternate, Is.Not.Null);
        var camera = Camera.main;
        var rig = camera.GetComponent<AngleSnapCamera>();
        rig.SetAnchorIndexForTest(0);
        yield return new WaitForSeconds(0.3f);
        var firstRotation = camera.transform.rotation;
        rig.SetAnchorIndexForTest(1);
        yield return new WaitForSeconds(0.3f);
        Assert.That(rig.LockedAngleId, Is.EqualTo(1));
        Assert.That(Quaternion.Angle(firstRotation, camera.transform.rotation),
            Is.GreaterThan(45f));
        var player = Object.FindFirstObjectByType<PlayerPathController>();
        var viewport = camera.WorldToViewportPoint(player.transform.position);
        Assert.That(viewport.x, Is.InRange(0.08f, 0.92f));
        Assert.That(viewport.y, Is.InRange(0.08f, 0.92f));
    }

    [UnityTest]
    public IEnumerator Outer_rim_trap_can_be_used_without_entering_the_patrol_row()
    {
        yield return SceneManager.LoadSceneAsync(SceneName);
        yield return null;
        var board = Object.FindFirstObjectByType<GridBoard>();
        var player = Object.FindFirstObjectByType<PlayerPathController>();
        var interactor = player.GetComponent<PlayerInteractor>();
        var hazard = GameObject.Find("Stage03_TrapConsole").GetComponent<EnvironmentalHazard>();
        var enemy = GameObject.Find("Stage03_PatrolEnemy").GetComponent<PatrolEnemy>();
        Assert.That(board.TryGetCell(new Vector2Int(19, 8), out var rim), Is.True);
        Assert.That(player.RestoreToNearestNode(rim.Node.Position, Quaternion.identity), Is.True);
        yield return null;
        Assert.That(interactor.TryInteractNow(), Is.True);
        yield return new WaitForSeconds(0.8f);
        Assert.That(hazard.IsSpent, Is.True);
        Assert.That(enemy.IsCleared, Is.True);
    }

    [UnityTest]
    public IEnumerator F_preview_reveals_only_the_nearby_shared_spawn_pad()
    {
        yield return SceneManager.LoadSceneAsync(SceneName);
        yield return null;
        var board = Object.FindFirstObjectByType<GridBoard>();
        var player = Object.FindFirstObjectByType<PlayerPathController>();
        var placement = player.GetComponent<ShadowPlacement>();

        Assert.That(board.TryGetCell(new Vector2Int(3, 6), out var westernPad), Is.True);
        Assert.That(placement.BeginPreview(), Is.True);
        Assert.That(placement.Preview.VisibleCount, Is.EqualTo(1));
        var dot = placement.Preview.transform.Find("ValidShadowCell_0");
        Assert.That(dot, Is.Not.Null);
        Assert.That(dot.localScale.x, Is.GreaterThanOrEqualTo(0.4f),
            "A legal F-placement dot must read clearly from the wide courtyard view.");
        Assert.That(dot.GetComponent<MeshRenderer>().sharedMaterial.color.r,
            Is.GreaterThan(0.9f), "The dot must contrast with the cyan spawn pad.");
        placement.UpdatePreview(new Ray(westernPad.SurfacePoint + Vector3.up * 8f,
            Vector3.down));
        Assert.That(placement.Preview.HoveredCell, Is.SameAs(westernPad));
        Assert.That(placement.EndPreview(), Is.True);

        Assert.That(board.TryGetCell(new Vector2Int(14, 6), out var eastStart), Is.True);
        Assert.That(player.RestoreToNearestNode(eastStart.Node.Position, Quaternion.identity),
            Is.True);
        Assert.That(board.TryGetCell(new Vector2Int(15, 6), out var easternPad), Is.True);
        Assert.That(placement.BeginPreview(), Is.True);
        Assert.That(placement.Preview.VisibleCount, Is.EqualTo(1));
        placement.UpdatePreview(new Ray(easternPad.SurfacePoint + Vector3.up * 8f,
            Vector3.down));
        Assert.That(placement.Preview.HoveredCell, Is.SameAs(easternPad));
        Assert.That(placement.EndPreview(), Is.True);
    }

    [UnityTest]
    public IEnumerator East_checkpoint_restores_player_and_only_the_saved_mechanism_state()
    {
        yield return SceneManager.LoadSceneAsync(SceneName);
        yield return null;
        var board = Object.FindFirstObjectByType<GridBoard>();
        var player = Object.FindFirstObjectByType<PlayerPathController>();
        var service = Object.FindFirstObjectByType<CheckpointService>();
        var marker = GameObject.Find("Stage03_Checkpoint").GetComponent<CheckpointMarker>();
        var plateA = GameObject.Find("Stage03_PlateA").GetComponent<PressurePlate>();
        var plateB = GameObject.Find("Stage03_PlateB").GetComponent<PressurePlate>();
        var gateA = GameObject.Find("Stage03_GateA").GetComponent<StageGate>();
        var gateB = GameObject.Find("Stage03_GateB").GetComponent<StageGate>();
        var rotor = GameObject.Find("Stage03_RotorConsole").GetComponent<RotationModule>();

        plateA.SetPressedForTest(true);
        yield return null;
        service.Activate(marker);
        plateB.SetPressedForTest(true);
        yield return null;
        rotor.RotateNext();
        Assert.That(board.TryGetCell(new Vector2Int(18, 8), out var eastCell), Is.True);
        Assert.That(player.RestoreToNearestNode(eastCell.Node.Position, Quaternion.identity), Is.True);
        service.NotifyPlayerFailure();

        Assert.That(board.TryGetCell(new Vector2Int(14, 6), out var checkpointCell), Is.True);
        Assert.That(player.CurrentNode, Is.SameAs(checkpointCell.Node));
        Assert.That(plateA.IsPressed, Is.True);
        Assert.That(gateA.IsOpen, Is.True);
        Assert.That(plateB.IsPressed, Is.False);
        Assert.That(gateB.IsOpen, Is.False);
        Assert.That(rotor.CurrentIndex, Is.Zero);
    }

    [UnityTest]
    public IEnumerator Gates_and_rotor_force_the_south_return_before_the_north_exit()
    {
        Assert.That(Application.CanStreamedLevelBeLoaded(SceneName), Is.True,
            "Stage 03 must be enabled in Build Settings.");
        var load = SceneManager.LoadSceneAsync(SceneName);
        yield return new WaitUntil(() => load.isDone);

        var board = Object.FindFirstObjectByType<GridBoard>();
        var graph = Object.FindFirstObjectByType<PathGraph>();
        var rotor = GameObject.Find("Stage03_RotorConsole").GetComponent<RotationModule>();
        var plateA = GameObject.Find("Stage03_PlateA").GetComponent<PressurePlate>();
        var plateB = GameObject.Find("Stage03_PlateB").GetComponent<PressurePlate>();
        var gateA = GameObject.Find("Stage03_GateA").GetComponent<StageGate>();
        var gateB = GameObject.Find("Stage03_GateB").GetComponent<StageGate>();
        PathNode At(int x, int y)
        {
            Assert.That(board.TryGetCell(new Vector2Int(x, y), out var cell), Is.True);
            return cell.Node;
        }

        Assert.That(Pathfinder.FindPath(graph, At(2, 8), At(14, 6)), Is.Null,
            "Gate I must prevent crossing even though the rotor is horizontal.");
        plateA.SetPressedForTest(true);
        yield return null;
        Assert.That(gateA.IsOpen, Is.True);
        Assert.That(Pathfinder.FindPath(graph, At(2, 8), At(14, 6)), Is.Not.Null);

        Assert.That(Pathfinder.FindPath(graph, At(18, 9), At(11, 9)), Is.Null,
            "Gate II must have no route around it from another eastern tile.");
        plateB.SetPressedForTest(true);
        yield return null;
        Assert.That(gateB.IsOpen, Is.True);
        Assert.That(Pathfinder.FindPath(graph, At(18, 9), At(11, 9)), Is.Not.Null);
        Assert.That(Pathfinder.FindPath(graph, At(11, 9), At(11, 1)), Is.Null);

        rotor.RotateNext();
        Assert.That(Pathfinder.FindPath(graph, At(11, 9), At(11, 1)), Is.Not.Null,
            "Vertical rotor must connect the southern port to the north exit.");
        Assert.That(Pathfinder.FindPath(graph, At(9, 6), At(13, 6)), Is.Null,
            "The east-west shortcut must close when the bridge rotates north-south.");
    }

    [UnityTest]
    public IEnumerator Shadow_spawns_only_on_shared_pad_and_walks_into_western_plate()
    {
        var load = SceneManager.LoadSceneAsync(SceneName);
        yield return new WaitUntil(() => load.isDone);
        yield return null;

        var board = Object.FindFirstObjectByType<GridBoard>();
        var player = Object.FindFirstObjectByType<PlayerPathController>();
        var placement = player.GetComponent<ShadowPlacement>();
        var shadow = player.GetComponent<ShadowController>();
        var plate = GameObject.Find("Stage03_PlateA").GetComponent<PressurePlate>();
        var gate = GameObject.Find("Stage03_GateA").GetComponent<StageGate>();
        Assert.That(board.TryGetCell(new Vector2Int(6, 3), out var plateCell), Is.True);
        Assert.That(board.TryGetCell(new Vector2Int(3, 6), out var pad), Is.True);
        Assert.That(placement.TryPlace(plateCell), Is.False,
            "The plate alcove must not be a direct F-placement shortcut.");
        Assert.That(placement.TryPlace(pad), Is.True);
        yield return new WaitForSeconds(0.5f);
        Assert.That(shadow.HasShadow, Is.True);

        yield return MoveTo(player, board, 5, 8);
        yield return MoveTo(player, board, 5, 5);
        yield return new WaitForFixedUpdate();
        Assert.That(plate.IsPressed, Is.True,
            $"Moving through the shared courtyard should reach plate I; shadow={shadow.ActiveShadow.transform.position}.");
        Assert.That(gate.IsOpen, Is.True);
    }

    [UnityTest]
    public IEnumerator Eastern_shadow_plate_opens_south_return_and_rotor_reaches_exit()
    {
        var load = SceneManager.LoadSceneAsync(SceneName);
        yield return new WaitUntil(() => load.isDone);
        yield return null;

        var board = Object.FindFirstObjectByType<GridBoard>();
        var player = Object.FindFirstObjectByType<PlayerPathController>();
        var placement = player.GetComponent<ShadowPlacement>();
        var shadow = player.GetComponent<ShadowController>();
        var plateA = GameObject.Find("Stage03_PlateA").GetComponent<PressurePlate>();
        var plateB = GameObject.Find("Stage03_PlateB").GetComponent<PressurePlate>();
        var gateA = GameObject.Find("Stage03_GateA").GetComponent<StageGate>();
        var gateB = GameObject.Find("Stage03_GateB").GetComponent<StageGate>();
        var rotor = GameObject.Find("Stage03_RotorConsole").GetComponent<RotationModule>();
        var exit = GameObject.Find("Stage03_Exit").GetComponent<StageExit>();
        var checkpoint = Object.FindFirstObjectByType<CheckpointService>();
        var marker = GameObject.Find("Stage03_Checkpoint").GetComponent<CheckpointMarker>();

        Assert.That(board.TryGetCell(new Vector2Int(3, 6), out var westernPad), Is.True);
        Assert.That(placement.TryPlace(westernPad), Is.True);
        yield return new WaitForSeconds(0.5f);
        yield return MoveTo(player, board, 5, 8);
        yield return MoveTo(player, board, 5, 5);
        Assert.That(plateA.IsPressed, Is.True);
        Assert.That(gateA.IsOpen, Is.True);

        yield return MoveTo(player, board, 14, 6);
        Assert.That(checkpoint.ActiveMarker, Is.SameAs(marker),
            "Crossing into the east courtyard must actually trigger the checkpoint.");
        yield return MoveTo(player, board, 14, 8);
        Assert.That(board.TryGetCell(new Vector2Int(18, 2), out var plateCell), Is.True);
        Assert.That(board.TryGetCell(new Vector2Int(15, 6), out var pad), Is.True);
        Assert.That(placement.TryPlace(plateCell), Is.False);
        Assert.That(placement.TryPlace(pad), Is.True);
        yield return new WaitForSeconds(0.5f);
        yield return MoveTo(player, board, 17, 8);
        yield return MoveTo(player, board, 17, 4);
        yield return new WaitForFixedUpdate();
        Assert.That(plateB.IsPressed, Is.True,
            $"Shadow should enter the northern alcove from the shared east floor; shadow={shadow.ActiveShadow.transform.position}.");
        Assert.That(gateB.IsOpen, Is.True);

        // The east patrol guards the short diagonal toward Gate II; take the
        // outer rim before joining the south return corridor.
        yield return MoveTo(player, board, 19, 8);
        yield return MoveTo(player, board, 19, 9);
        yield return MoveTo(player, board, 18, 9);
        yield return MoveTo(player, board, 18, 11);
        yield return MoveTo(player, board, 11, 9);
        rotor.RotateNext();
        yield return MoveTo(player, board, 11, 1);
        yield return new WaitForFixedUpdate();
        Assert.That(exit.IsComplete, Is.True);
        yield return null;
        Assert.That(GameObject.Find("Stage03GoalText").GetComponent<TMP_Text>().text,
            Does.Contain("第三关完成"));
    }

    private static IEnumerator MoveTo(PlayerPathController player, GridBoard board, int x, int y)
    {
        Assert.That(board.TryGetCell(new Vector2Int(x, y), out var cell), Is.True);
        Assert.That(player.TryMoveTo(cell.Node), Is.True, $"Route to ({x},{y}) must be available.");
        var deadline = Time.time + 20f;
        while (player.IsMoving && Time.time < deadline) yield return null;
        Assert.That(player.IsMoving, Is.False, $"Travel to ({x},{y}) timed out.");
        var currentCell = player.CurrentNode != null
            ? player.CurrentNode.GetComponentInParent<GridCell>() : null;
        Assert.That(player.CurrentNode, Is.SameAs(cell.Node),
            $"Player stopped before ({x},{y}); current cell={currentCell?.Coordinate}, position={player.transform.position}.");
    }
}
