using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class Stage02FlowTests
{
    [UnityTest]
    public IEnumerator Walking_onto_stage_two_checkpoint_activates_it()
    {
        var load = SceneManager.LoadSceneAsync("Stage02_TurningPursuit");
        yield return new WaitUntil(() => load.isDone);

        var board = Object.FindFirstObjectByType<GridBoard>();
        var player = Object.FindFirstObjectByType<PlayerPathController>();
        var service = Object.FindFirstObjectByType<CheckpointService>();
        Object.FindFirstObjectByType<RotationModule>().RotateNext();
        Assert.That(board.TryGetCell(new Vector2Int(8, 0), out var checkpointCell), Is.True);
        Assert.That(player.TryMoveTo(checkpointCell.Node), Is.True);
        var deadline = Time.time + 12f;
        while (player.IsMoving && Time.time < deadline) yield return null;
        yield return new WaitForFixedUpdate();
        Assert.That(player.CurrentNode, Is.SameAs(checkpointCell.Node));
        Assert.That(service.ActiveMarker, Is.Not.Null,
            "A normal graph walk across the checkpoint must save progress.");
    }

    [UnityTest]
    public IEnumerator Checkpoint_restores_rotor_gates_enemy_and_hazard()
    {
        var load = SceneManager.LoadSceneAsync("Stage02_TurningPursuit");
        yield return new WaitUntil(() => load.isDone);

        var module = Object.FindFirstObjectByType<RotationModule>();
        var plate = Object.FindFirstObjectByType<PressurePlate>();
        var shadowGate = GameObject.Find("Stage02_ShadowGate").GetComponent<StageGate>();
        var hazardGate = GameObject.Find("Stage02_HazardGate").GetComponent<StageGate>();
        var enemy = Object.FindFirstObjectByType<PatrolEnemy>();
        var hazard = Object.FindFirstObjectByType<EnvironmentalHazard>();
        var service = Object.FindFirstObjectByType<CheckpointService>();
        var marker = GameObject.Find("Stage02_Checkpoint").GetComponent<CheckpointMarker>();

        module.RotateNext();
        service.Activate(marker);
        var savedEnemyPosition = enemy.transform.position;
        module.RotateNext();
        plate.SetPressedForTest(true);
        hazard.TriggerForTest(enemy);
        Assert.That(shadowGate.IsOpen && hazardGate.IsOpen && enemy.IsCleared && hazard.IsSpent, Is.True);

        service.RestoreLatestCheckpoint();
        Assert.That(module.CurrentIndex, Is.EqualTo(1));
        Assert.That(plate.IsPressed, Is.False);
        Assert.That(shadowGate.IsOpen || hazardGate.IsOpen, Is.False);
        Assert.That(shadowGate.GetComponent<Collider>().enabled, Is.True);
        Assert.That(hazardGate.GetComponent<Collider>().enabled, Is.True);
        Assert.That(enemy.IsCleared, Is.False);
        Assert.That(enemy.GetComponent<Collider>().enabled, Is.True);
        Assert.That(Vector3.Distance(enemy.transform.position, savedEnemyPosition), Is.LessThan(0.01f));
        Assert.That(hazard.IsSpent, Is.False);
    }

    [UnityTest]
    public IEnumerator Tutorial_keeps_next_objective_after_shadow_gate_opens()
    {
        var load = SceneManager.LoadSceneAsync("Stage02_TurningPursuit");
        yield return new WaitUntil(() => load.isDone);
        var module = Object.FindFirstObjectByType<RotationModule>();
        var plate = Object.FindFirstObjectByType<PressurePlate>();
        var gate = GameObject.Find("Stage02_ShadowGate").GetComponent<StageGate>();
        var text = GameObject.Find("GoalText").GetComponent<TMP_Text>();
        module.RotateNext();
        plate.SetPressedForTest(true);
        plate.SetPressedForTest(false);
        yield return null;
        Assert.That(gate.IsOpen, Is.True);
        Assert.That(text.text, Does.Contain("陷阱"),
            "The tutorial should progress with the latched gate, not rewind with plate contact.");
    }

    [UnityTest]
    public IEnumerator Shadow_follows_grid_travel_onto_plate_and_opens_gate()
    {
        var load = SceneManager.LoadSceneAsync("Stage02_TurningPursuit");
        yield return new WaitUntil(() => load.isDone);

        var board = Object.FindFirstObjectByType<GridBoard>();
        var module = Object.FindFirstObjectByType<RotationModule>();
        var player = Object.FindFirstObjectByType<PlayerPathController>();
        var placement = player.GetComponent<ShadowPlacement>();
        var shadow = player.GetComponent<ShadowController>();
        var plate = Object.FindFirstObjectByType<PressurePlate>();
        var gate = GameObject.Find("Stage02_ShadowGate").GetComponent<StageGate>();
        Assert.That(board.TryGetCell(new Vector2Int(7, 0), out var landing), Is.True);
        Assert.That(board.TryGetCell(new Vector2Int(8, 2), out var spawnCell), Is.True);
        Assert.That(board.TryGetCell(new Vector2Int(9, 0), out var playerGoal), Is.True);
        module.RotateNext();
        Assert.That(player.TryMoveTo(landing.Node), Is.True);
        var deadline = Time.time + 15f;
        while (player.IsMoving && Time.time < deadline) yield return null;
        Assert.That(player.CurrentNode, Is.SameAs(landing.Node));

        Assert.That(placement.TryPlace(spawnCell), Is.True,
            "The remote shadow lane should be a valid placement target from the landing.");
        yield return new WaitForSeconds(0.5f);
        Assert.That(shadow.HasShadow, Is.True);
        Assert.That(shadow.ActiveShadow.isActiveAndEnabled, Is.True);
        Assert.That(player.TryMoveTo(playerGoal.Node), Is.True);
        deadline = Time.time + 10f;
        while (player.IsMoving && Time.time < deadline) yield return null;
        yield return new WaitForFixedUpdate();
        Assert.That(player.CurrentNode, Is.SameAs(playerGoal.Node));
        var shadowPosition = shadow.ActiveShadow.transform.position;
        Assert.That(plate.IsPressed, Is.True,
            $"Player travel should drag the independent shadow onto the remote plate. "
            + $"Shadow={shadowPosition}, plate={plate.transform.position}, "
            + $"shadow layer={shadow.ActiveShadow.gameObject.layer}, "
            + $"player={player.transform.position}");
        Assert.That(gate.GetComponent<Collider>().enabled, Is.False);
    }

    [UnityTest]
    public IEnumerator Rotating_bridge_shadow_plate_and_hazard_unlock_the_exit()
    {
        var load = SceneManager.LoadSceneAsync("Stage02_TurningPursuit");
        yield return new WaitUntil(() => load.isDone);

        var graph = Object.FindFirstObjectByType<PathGraph>();
        var board = Object.FindFirstObjectByType<GridBoard>();
        var module = Object.FindFirstObjectByType<RotationModule>();
        var plate = Object.FindFirstObjectByType<PressurePlate>();
        var enemy = Object.FindFirstObjectByType<PatrolEnemy>();
        var hazard = Object.FindFirstObjectByType<EnvironmentalHazard>();
        var player = Object.FindFirstObjectByType<PlayerPathController>();
        var exit = Object.FindFirstObjectByType<StageExit>();
        Assert.That(graph != null && board != null && module != null && plate != null
            && enemy != null && hazard != null && player != null && exit != null, Is.True);
        Assert.That(board.TryGetCell(new Vector2Int(0, 0), out var start), Is.True);
        Assert.That(board.TryGetCell(new Vector2Int(7, 0), out var beyondRotor), Is.True);
        Assert.That(board.TryGetCell(new Vector2Int(14, 0), out var finish), Is.True);

        Assert.That(Pathfinder.FindPath(graph, start.Node, beyondRotor.Node), Is.Null,
            "The turntable must begin disconnected.");
        module.RotateNext();
        Assert.That(Pathfinder.FindPath(graph, start.Node, beyondRotor.Node), Is.Not.Null,
            "A quarter-turn should connect the manual endpoint edges.");
        Assert.That(Pathfinder.FindPath(graph, start.Node, finish.Node), Is.Null);

        plate.SetPressedForTest(true);
        yield return null;
        Assert.That(Pathfinder.FindPath(graph, start.Node, finish.Node), Is.Null,
            "The enemy hazard still gates the final corridor.");
        hazard.TriggerForTest(enemy);
        yield return null;
        Assert.That(enemy.IsCleared, Is.True);
        Assert.That(hazard.IsSpent, Is.True);
        Assert.That(Pathfinder.FindPath(graph, start.Node, finish.Node), Is.Not.Null);

        Assert.That(player.TryMoveTo(finish.Node), Is.True);
        var deadline = Time.time + 20f;
        while (!exit.IsComplete && Time.time < deadline) yield return null;
        Assert.That(exit.IsComplete, Is.True);
    }
}
