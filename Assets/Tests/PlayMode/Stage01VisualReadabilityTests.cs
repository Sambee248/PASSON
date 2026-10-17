using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class Stage01VisualReadabilityTests
{
    [UnityTest]
    public IEnumerator Grid_tiles_have_noncolliding_depth_and_a_compact_tutorial()
    {
        var load = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => load.isDone);

        var board = GameObject.Find("Stage01_GridBoard");
        Assert.That(board, Is.Not.Null);
        foreach (var name in new[] { "Tile_00_00", "Tile_08_02", "Tile_17_00" })
        {
            var tile = board.transform.Find(name);
            Assert.That(tile, Is.Not.Null, name);
            var foundation = tile.Find("Foundation");
            Assert.That(foundation, Is.Not.Null, $"{name} should read as a solid chessboard square.");
            Assert.That(foundation.GetComponent<Renderer>(), Is.Not.Null);
            Assert.That(foundation.GetComponent<Collider>(), Is.Null,
                "Decorative depth must not change path or shadow collision.");
            Assert.That(foundation.position.y, Is.LessThan(tile.GetComponent<GridCell>().SurfacePoint.y));
        }

        var hud = GameObject.Find("Stage01_T7_TutorialHUD");
        Assert.That(hud, Is.Not.Null);
        var backdrop = hud.transform.Find("TutorialBackdrop").GetComponent<RectTransform>();
        var message = hud.transform.Find("TutorialMessage").GetComponent<TMP_Text>();
        Assert.That(backdrop.sizeDelta.x, Is.LessThanOrEqualTo(900f));
        Assert.That(backdrop.sizeDelta.y, Is.LessThanOrEqualTo(80f));
        Assert.That(backdrop.GetComponent<Image>().color.a, Is.LessThanOrEqualTo(0.68f));
        Assert.That(message.fontSize, Is.LessThanOrEqualTo(25f));
    }
}
