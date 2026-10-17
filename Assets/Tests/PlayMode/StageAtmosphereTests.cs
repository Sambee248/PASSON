using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class StageAtmosphereTests
{
    [UnityTest]
    public IEnumerator First_stage_uses_a_dark_solid_backdrop()
    {
        yield return SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        AssertDarkBackdrop();
    }

    [UnityTest]
    public IEnumerator Second_stage_uses_a_dark_solid_backdrop()
    {
        yield return SceneManager.LoadSceneAsync("Stage02_TurningPursuit");
        AssertDarkBackdrop();
    }

    private static void AssertDarkBackdrop()
    {
        var camera = Camera.main;
        Assert.That(camera, Is.Not.Null);
        Assert.That(camera.clearFlags, Is.EqualTo(CameraClearFlags.SolidColor));
        Assert.That(camera.backgroundColor.grayscale, Is.LessThan(0.15f));
    }
}
