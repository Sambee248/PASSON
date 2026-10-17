using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class Stage01ClickFeedbackTests
{
    [UnityTest]
    public IEnumerator Reachable_and_blocked_grid_clicks_have_distinct_noncolliding_feedback()
    {
        var scene = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => scene.isDone);

        var player = GameObject.Find("Stage01_T7_Player");
        var controller = player.GetComponent<PlayerPathController>();
        var feedback = player.GetComponent<PathClickFeedback>();
        var graph = Object.FindFirstObjectByType<PathGraph>();
        var board = Object.FindFirstObjectByType<GridBoard>();
        Assert.That(feedback, Is.Not.Null);

        Assert.That(controller.TryMoveTo(graph.Nodes[17]), Is.False,
            "The closed gate must keep the far destination unreachable.");
        Assert.That(feedback.RejectedVisible, Is.True);
        Assert.That(feedback.DestinationVisible, Is.False);
        Assert.That(board.TryGetCell(new Vector2Int(8, 2), out var shadowCell), Is.True);
        Assert.That(controller.TryMoveToWorldPoint(shadowCell.SurfacePoint), Is.False);
        Assert.That(feedback.RejectedVisible, Is.True);

        Assert.That(controller.TryMoveTo(graph.Nodes[1]), Is.True);
        Assert.That(feedback.DestinationVisible, Is.True);
        Assert.That(feedback.RejectedVisible, Is.False);
        Assert.That(feedback.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(1),
            "Feedback markers must add no collider beyond the player's CharacterController.");

        var deadline = Time.time + 2f;
        while (controller.IsMoving && Time.time < deadline) yield return null;
        Assert.That(feedback.DestinationVisible, Is.False);
    }
}
