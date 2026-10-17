using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class AngleSnapCameraTests
{
    [Test]
    public void Target_within_tolerance_snaps_to_matching_anchor()
    {
        Assert.That(AngleSnapCamera.IsWithinTolerance(28f, 30f, 3f), Is.True);
        Assert.That(AngleSnapCamera.IsWithinTolerance(24f, 30f, 3f), Is.False);
    }

    [Test]
    public void Authored_angle_selection_uses_global_Z_and_X_bindings()
    {
        Assert.That(AngleSnapCamera.PreviousAngleKey, Is.EqualTo(KeyCode.Z));
        Assert.That(AngleSnapCamera.NextAngleKey, Is.EqualTo(KeyCode.X));
    }

    [UnityTest]
    public IEnumerator Explicit_selection_interpolates_and_locks_anchor_one()
    {
        var cameraObject = new GameObject("Camera");
        var anchorZero = new GameObject("AnchorZero");
        var anchorOne = new GameObject("AnchorOne");
        var camera = cameraObject.AddComponent<AngleSnapCamera>();

        anchorOne.transform.SetPositionAndRotation(
            new Vector3(4f, 2f, 3f),
            Quaternion.Euler(15f, 150f, 0f));
        camera.ConfigureAnchorsForTest(anchorZero.transform, anchorOne.transform);
        camera.SetSnapDurationForTest(0f);

        camera.SetAnchorIndexForTest(1);
        yield return null;

        Assert.That(camera.IsLocked, Is.True);
        Assert.That(camera.LockedAngleId, Is.EqualTo(1));
        Assert.That(camera.transform.position, Is.EqualTo(anchorOne.transform.position));
        Assert.That(camera.transform.rotation, Is.EqualTo(anchorOne.transform.rotation));

        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(anchorZero);
        Object.DestroyImmediate(anchorOne);
    }
}
