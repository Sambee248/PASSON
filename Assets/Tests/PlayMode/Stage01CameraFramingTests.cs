using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class Stage01CameraFramingTests
{
    [UnityTest]
    public IEnumerator Locked_camera_follows_the_player_across_the_long_grid()
    {
        var scene = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => scene.isDone);

        var cameraSnap = Object.FindFirstObjectByType<AngleSnapCamera>();
        var camera = cameraSnap.GetComponent<Camera>();
        var path = GameObject.Find("Stage01_T7_Player").GetComponent<PlayerPathController>();
        var graph = Object.FindFirstObjectByType<PathGraph>();

        Assert.That(camera.fieldOfView, Is.LessThanOrEqualTo(50f),
            "The 1.5-unit tiles should fill enough of the Game view to be readable.");
        cameraSnap.SetAnchorIndexForTest(0);
        yield return new WaitForSeconds(0.4f);
        var cameraAtStart = camera.transform.position.x;

        Assert.That(path.RestoreToNearestNode(graph.Nodes[16].Position, Quaternion.identity), Is.True);
        yield return new WaitForSeconds(0.7f);

        Assert.That(cameraSnap.IsLocked, Is.True, "Following must not clear the solved viewpoint.");
        Assert.That(camera.transform.position.x - cameraAtStart, Is.GreaterThan(5f),
            "The exit half of the board should not leave the player stranded at the screen edge.");
        var viewport = camera.WorldToViewportPoint(path.transform.position);
        Assert.That(viewport.x, Is.GreaterThan(0.1f).And.LessThan(0.9f));
    }
}
