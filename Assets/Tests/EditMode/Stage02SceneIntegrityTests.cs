using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

public sealed class Stage02SceneIntegrityTests
{
    [Test]
    public void Saved_stage_has_no_missing_script_components()
    {
        var scene = EditorSceneManager.OpenScene(
            "Assets/Scenes/Stage02_TurningPursuit.unity", OpenSceneMode.Single);
        var missing = new List<string>();
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                var count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                    transform.gameObject);
                if (count > 0) missing.Add($"{transform.name} ({count})");
            }
        }
        Assert.That(missing, Is.Empty);
        Assert.That(GameObject.Find("Stage02_Checkpoint").GetComponent<CheckpointMarker>(),
            Is.Not.Null, "The checkpoint script must resolve in the saved scene.");
    }

    [Test]
    public void Camera_elevation_exposes_both_grid_rows_and_turntable()
    {
        EditorSceneManager.OpenScene(
            "Assets/Scenes/Stage02_TurningPursuit.unity", OpenSceneMode.Single);
        var anchor = GameObject.Find("Stage02_CameraAnchor");
        var target = GameObject.Find("Stage02_CameraTarget");
        Assert.That(anchor, Is.Not.Null);
        Assert.That(target, Is.Not.Null);
        var elevation = Mathf.Atan2(anchor.transform.position.y - target.transform.position.y,
            Mathf.Abs(anchor.transform.position.z - target.transform.position.z))
            * Mathf.Rad2Deg;
        Assert.That(elevation, Is.GreaterThan(35f));
    }

    [Test]
    public void Saved_stage_marks_shadow_plate_and_hazard_console_with_distinct_visible_labels()
    {
        EditorSceneManager.OpenScene(
            "Assets/Scenes/Stage02_TurningPursuit.unity", OpenSceneMode.Single);
        var plate = GameObject.Find("Stage02_ShadowPlate");
        var console = GameObject.Find("Stage02_HazardConsole");
        Assert.That(plate, Is.Not.Null);
        Assert.That(console, Is.Not.Null);

        var plateLabel = GameObject.Find("Stage02_ShadowPlateLabel")?.transform;
        var trapLabel = GameObject.Find("Stage02_HazardLabel")?.transform;
        Assert.That(plateLabel, Is.Not.Null, "The pressure plate needs a world-space marker.");
        Assert.That(trapLabel, Is.Not.Null, "The hazard console needs a world-space marker.");
        Assert.That(plateLabel.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.WorldSpace));
        Assert.That(trapLabel.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.WorldSpace));
        StringAssert.Contains("影子踏板", plateLabel.GetComponentInChildren<TextMeshProUGUI>().text);
        StringAssert.Contains("陷阱", trapLabel.GetComponentInChildren<TextMeshProUGUI>().text);
        StringAssert.Contains("E", trapLabel.GetComponentInChildren<TextMeshProUGUI>().text);
        Assert.That(plateLabel.GetComponentsInChildren<Collider>(), Is.Empty);
        Assert.That(trapLabel.GetComponentsInChildren<Collider>(), Is.Empty);
    }

    [Test]
    public void Device_marker_tracks_its_anchor_and_faces_the_camera()
    {
        EditorSceneManager.OpenScene(
            "Assets/Scenes/Stage02_TurningPursuit.unity", OpenSceneMode.Single);
        var console = GameObject.Find("Stage02_HazardConsole");
        var marker = GameObject.Find("Stage02_HazardLabel");
        Assert.That(marker, Is.Not.Null);
        var camera = Camera.main;
        var previousCameraRotation = camera.transform.rotation;
        var previousConsolePosition = console.transform.position;
        var previousMarkerPosition = marker.transform.position;
        try
        {
            var delta = new Vector3(1.2f, 0.3f, -0.4f);
            console.transform.position += delta;
            camera.transform.rotation = Quaternion.Euler(30f, 55f, 0f);
            marker.GetComponent<WorldDeviceMarker>().RefreshPose();
            Assert.That(Vector3.Distance(marker.transform.position,
                previousMarkerPosition + delta), Is.LessThan(0.01f));
            Assert.That(Quaternion.Angle(marker.transform.rotation,
                camera.transform.rotation), Is.LessThan(0.1f));
        }
        finally
        {
            console.transform.position = previousConsolePosition;
            camera.transform.rotation = previousCameraRotation;
        }
    }
}
