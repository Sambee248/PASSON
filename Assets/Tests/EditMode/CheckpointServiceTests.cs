using NUnit.Framework;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public sealed class CheckpointServiceTests
{
    [Test]
    public void State_round_trip_restores_plate_activation()
    {
        var plate = new GameObject().AddComponent<PressurePlate>();
        plate.SetPressedForTest(true);
        var saved = plate.CaptureState();
        plate.SetPressedForTest(false);
        plate.RestoreState(saved);
        Assert.That(plate.IsPressed, Is.True);

        Object.DestroyImmediate(plate.gameObject);
    }

    [Test]
    public void Destroyed_and_recreated_shadow_colliders_do_not_leave_a_plate_pressed()
    {
        var plateObject = new GameObject("Plate");
        var plate = plateObject.AddComponent<PressurePlate>();
        var shadowLayer = LayerMask.NameToLayer("Shadow");
        var firstShadow = CreateShadowCollider("First shadow", shadowLayer);

        try
        {
            InvokeTrigger(plate, "OnTriggerEnter", firstShadow);
            Assert.That(plate.IsPressed, Is.True);

            Object.DestroyImmediate(firstShadow.gameObject);
            InvokeFixedUpdate(plate);
            Assert.That(plate.IsPressed, Is.False, "A destroyed overlap must be discarded without waiting for OnTriggerExit.");

            var replacementShadow = CreateShadowCollider("Replacement shadow", shadowLayer);
            try
            {
                InvokeTrigger(plate, "OnTriggerEnter", replacementShadow);
                Assert.That(plate.IsPressed, Is.True);

                Object.DestroyImmediate(replacementShadow.gameObject);
                InvokeFixedUpdate(plate);
                Assert.That(plate.IsPressed, Is.False);
            }
            finally
            {
                if (replacementShadow != null)
                {
                    Object.DestroyImmediate(replacementShadow.gameObject);
                }
            }
        }
        finally
        {
            Object.DestroyImmediate(plateObject);
        }
    }

    [Test]
    public void Shadow_only_plate_rejects_player_and_opens_its_linked_gate_for_shadow()
    {
        var plateObject = new GameObject("Shadow-only plate");
        var gateObject = new GameObject("Linked gate");
        var playerObject = new GameObject("Player actor");
        var shadowObject = new GameObject("Shadow actor");

        try
        {
            var plate = plateObject.AddComponent<PressurePlate>();
            var gate = gateObject.AddComponent<StageGate>();
            gateObject.AddComponent<BoxCollider>();
            SetEnum(plate, "acceptedActors", (int)AcceptedActors.Shadow);
            GetActivatedEvent(plate).AddListener(gate.Open);

            playerObject.layer = LayerMask.NameToLayer("Player");
            shadowObject.layer = LayerMask.NameToLayer("Shadow");
            var playerCollider = playerObject.AddComponent<BoxCollider>();
            var shadowCollider = shadowObject.AddComponent<BoxCollider>();

            InvokeTrigger(plate, "OnTriggerEnter", playerCollider);
            Assert.That(plate.IsPressed, Is.False);
            Assert.That(gate.IsOpen, Is.False, "A player must not activate the shadow-only plate.");

            InvokeTrigger(plate, "OnTriggerEnter", shadowCollider);
            Assert.That(plate.IsPressed, Is.True);
            Assert.That(gate.IsOpen, Is.True, "The linked gate must open from the shadow's real plate activation.");
        }
        finally
        {
            Object.DestroyImmediate(shadowObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(gateObject);
            Object.DestroyImmediate(plateObject);
        }
    }

    [Test]
    public void Plate_ignores_an_unlayered_collider_when_the_shadow_tag_is_not_defined()
    {
        var plateObject = new GameObject("Plate");
        var unrelatedObject = new GameObject("Unrelated collider");

        try
        {
            var plate = plateObject.AddComponent<PressurePlate>();
            var unrelatedCollider = unrelatedObject.AddComponent<BoxCollider>();

            Assert.DoesNotThrow(() => InvokeTrigger(plate, "OnTriggerEnter", unrelatedCollider));
            Assert.That(plate.IsPressed, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(unrelatedObject);
            Object.DestroyImmediate(plateObject);
        }
    }

    [Test]
    public void Activate_snapshots_registered_state_player_transform_and_shadow_configuration_for_restore()
    {
        var player = new GameObject("Player");
        var markerObject = new GameObject("Marker");
        var mechanismObject = new GameObject("Mechanism");
        var shadowObject = new GameObject("Shadow configuration");
        var serviceObject = new GameObject("Checkpoint service");

        try
        {
            player.transform.position = new Vector3(9f, 2f, -3f);
            markerObject.transform.position = new Vector3(1f, 0f, 4f);
            markerObject.AddComponent<BoxCollider>();
            var marker = markerObject.AddComponent<CheckpointMarker>();
            var mechanism = mechanismObject.AddComponent<CheckpointStateProbe>();
            mechanism.IsActive = true;
            var shadow = shadowObject.AddComponent<ShadowConfigurationProbe>();
            shadow.CurrentConfiguration = "saved-shadow";
            var service = serviceObject.AddComponent<CheckpointService>();
            SetReference(service, "playerTransform", player.transform);
            SetReference(service, "shadowConfigurationSource", shadow);

            service.Activate(marker);
            Assert.That(service.RegisteredCheckpointCount, Is.GreaterThanOrEqualTo(1),
                "The probe must be registered even when an authored scene is open in the editor.");

            player.transform.position = Vector3.one * 99f;
            mechanism.IsActive = false;
            shadow.CurrentConfiguration = "changed-shadow";
            service.RestoreLatestCheckpoint();

            Assert.That(player.transform.position, Is.EqualTo(markerObject.transform.position));
            Assert.That(mechanism.IsActive, Is.True);
            Assert.That(shadow.RestoredConfiguration, Is.EqualTo("saved-shadow"));
            Assert.That(shadow.RestoreCallCount, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(serviceObject);
            Object.DestroyImmediate(shadowObject);
            Object.DestroyImmediate(mechanismObject);
            Object.DestroyImmediate(markerObject);
            Object.DestroyImmediate(player);
        }
    }

    private static Collider CreateShadowCollider(string name, int shadowLayer)
    {
        var shadow = new GameObject(name) { layer = shadowLayer };
        return shadow.AddComponent<BoxCollider>();
    }

    private static void InvokeTrigger(PressurePlate plate, string methodName, Collider collider)
    {
        typeof(PressurePlate)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(plate, new object[] { collider });
    }

    private static void InvokeFixedUpdate(PressurePlate plate)
    {
        typeof(PressurePlate)
            .GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(plate, null);
    }

    private static void SetReference(Object target, string propertyName, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetEnum(Object target, string propertyName, int value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).enumValueIndex = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static UnityEvent GetActivatedEvent(PressurePlate plate)
    {
        return (UnityEvent)typeof(PressurePlate)
            .GetField("activated", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(plate);
    }

    private sealed class CheckpointStateProbe : MonoBehaviour, ICheckpointState
    {
        public bool IsActive { get; set; }

        public string CheckpointId => "checkpoint-state-probe";

        public string CaptureState()
        {
            return JsonUtility.ToJson(new State { isActive = IsActive });
        }

        public void RestoreState(string json)
        {
            IsActive = JsonUtility.FromJson<State>(json).isActive;
        }

        private struct State
        {
            public bool isActive;
        }
    }

    private sealed class ShadowConfigurationProbe : MonoBehaviour, ICheckpointActorConfiguration
    {
        public string CurrentConfiguration { get; set; }
        public string RestoredConfiguration { get; private set; }
        public int RestoreCallCount { get; private set; }

        public string CaptureActorConfiguration()
        {
            return CurrentConfiguration;
        }

        public void RestoreActorConfiguration(string json)
        {
            RestoredConfiguration = json;
            RestoreCallCount++;
        }
    }
}
