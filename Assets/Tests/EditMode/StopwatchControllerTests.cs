using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public sealed class StopwatchControllerTests
{
    [Test]
    public void Held_duration_is_clamped_to_the_supported_zero_to_four_second_range()
    {
        Assert.That(StopwatchController.ClampDelay(-1f), Is.EqualTo(0f));
        Assert.That(StopwatchController.ClampDelay(2.25f), Is.EqualTo(2.25f));
        Assert.That(StopwatchController.ClampDelay(5.2f), Is.EqualTo(4f));
    }

    [Test]
    public void Held_duration_under_click_threshold_resolves_to_real_time()
    {
        Assert.That(StopwatchController.ResolveSelectedDelay(0.099f), Is.EqualTo(0f));
        Assert.That(StopwatchController.ResolveSelectedDelay(0.1f), Is.EqualTo(0.1f));
    }

    [Test]
    public void Releasing_after_unlock_confirms_delay_updates_mode_and_notifies_hud()
    {
        var host = new GameObject("Stopwatch host");
        var shadow = host.AddComponent<ShadowController>();
        var stopwatch = host.AddComponent<StopwatchController>();
        var feedback = host.AddComponent<StopwatchFeedbackProbe>();
        var click = host.AddComponent<AudioSource>();
        var confirmedDelay = -1f;

        try
        {
            SetReference(stopwatch, "shadowController", shadow);
            SetReference(stopwatch, "stopwatchFeedback", feedback);
            SetReference(stopwatch, "clickAudioSource", click);
            InvokeAwake(stopwatch);
            stopwatch.DelayConfirmed += delay => confirmedDelay = delay;

            stopwatch.UnlockStopwatch();
            stopwatch.ConfirmHeldDuration(2.75f);

            Assert.That(feedback.Unlocked, Is.True);
            Assert.That(stopwatch.SelectedDelaySeconds, Is.EqualTo(2.75f));
            Assert.That(stopwatch.DelayModeActive, Is.True);
            Assert.That(confirmedDelay, Is.EqualTo(2.75f));
            Assert.That(feedback.LastDelay, Is.EqualTo(2.75f));
            Assert.That(feedback.LastSelecting, Is.False);
            Assert.That(feedback.LastDelayedMode, Is.True);
            Assert.That(GetPrivateFloat(shadow, "delaySeconds"), Is.EqualTo(2.75f));
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void Releasing_a_click_duration_restores_real_time_mode_and_hud_feedback()
    {
        var host = new GameObject("Stopwatch host");
        var shadow = host.AddComponent<ShadowController>();
        var stopwatch = host.AddComponent<StopwatchController>();
        var feedback = host.AddComponent<StopwatchFeedbackProbe>();
        var click = host.AddComponent<AudioSource>();

        try
        {
            SetReference(stopwatch, "shadowController", shadow);
            SetReference(stopwatch, "stopwatchFeedback", feedback);
            SetReference(stopwatch, "clickAudioSource", click);
            InvokeAwake(stopwatch);

            stopwatch.UnlockStopwatch();
            stopwatch.ConfirmHeldDuration(0.099f);

            Assert.That(stopwatch.SelectedDelaySeconds, Is.Zero);
            Assert.That(stopwatch.DelayModeActive, Is.False);
            Assert.That(feedback.LastDelay, Is.Zero);
            Assert.That(feedback.LastSelecting, Is.False);
            Assert.That(feedback.LastDelayedMode, Is.False);
            Assert.That(GetPrivateFloat(shadow, "delaySeconds"), Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void Delayed_mode_does_not_queue_movement_commands()
    {
        var player = new GameObject("Player");
        var controller = player.AddComponent<ShadowController>();
        var shadowPrefab = new GameObject("Shadow prefab");
        shadowPrefab.AddComponent<CharacterController>();
        shadowPrefab.AddComponent<ShadowFollower>();

        try
        {
            SetReference(controller, "shadowPrefab", shadowPrefab);
            Assert.That(controller.TrySpawn(Vector3.zero), Is.True);
            var positionBeforeCommands = controller.ActiveShadow.transform.position;
            controller.SetDelay(2f);

            Queue(controller, PlayerCommand.Move(1, Vector2.right));
            Queue(controller, PlayerCommand.Move(2, Vector2.up));
            Queue(controller, PlayerCommand.Move(3, Vector2.left));

            var scheduled = GetScheduledCommands(controller);
            Assert.That(scheduled, Is.Empty);
            Assert.That(controller.ActiveShadow.transform.position, Is.EqualTo(positionBeforeCommands));
        }
        finally
        {
            controller.Recall();
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(shadowPrefab);
        }
    }

    [Test]
    public void SetDelay_clamps_and_treats_click_duration_as_real_time_in_fixed_update()
    {
        var fixture = CreateShadowFixture();

        try
        {
            fixture.Controller.SetDelay(5.2f);
            Assert.That(GetPrivateFloat(fixture.Controller, "delaySeconds"), Is.EqualTo(StopwatchController.MaximumDelaySeconds));

            fixture.Controller.SetDelay(StopwatchController.ClickThresholdSeconds - 0.001f);
            Queue(fixture.Controller, PlayerCommand.Move(1, Vector2.right));
            InvokeFixedUpdate(fixture.Controller);

            Assert.That(GetQueueCount(fixture.Controller, "pendingCommands"), Is.Zero);
            Assert.That(fixture.Follower.transform.position, Is.EqualTo(fixture.InitialShadowPosition));
        }
        finally
        {
            DestroyShadowFixture(fixture);
        }
    }

    [Test]
    public void Switching_to_delayed_mode_before_fixed_update_does_not_strand_realtime_commands()
    {
        var fixture = CreateShadowFixture();

        try
        {
            fixture.Controller.SetDelay(0f);
            Queue(fixture.Controller, PlayerCommand.Move(1, Vector2.right));
            fixture.Controller.SetDelay(2f);

            InvokeFixedUpdate(fixture.Controller);

            Assert.That(fixture.Follower.transform.position, Is.EqualTo(fixture.InitialShadowPosition));
            Assert.That(GetQueueCount(fixture.Controller, "pendingCommands"), Is.Zero);
        }
        finally
        {
            DestroyShadowFixture(fixture);
        }
    }

    [Test]
    public void Reducing_delay_cannot_schedule_a_later_command_before_the_fifo_head()
    {
        var fixture = CreateShadowFixture();

        try
        {
            fixture.Controller.SetDelay(4f);
            Queue(fixture.Controller, PlayerCommand.Interact(1));
            fixture.Controller.SetDelay(1f);
            Queue(fixture.Controller, PlayerCommand.Interact(2));

            var scheduled = GetScheduledCommands(fixture.Controller);
            Assert.That(scheduled, Has.Count.EqualTo(2));
            Assert.That(GetExecuteAt(scheduled[1]), Is.GreaterThanOrEqualTo(GetExecuteAt(scheduled[0])));
        }
        finally
        {
            DestroyShadowFixture(fixture);
        }
    }

    [Test]
    public void Delayed_update_executes_all_due_commands_in_fifo_order()
    {
        var fixture = CreateShadowFixture();
        var receiverObject = new GameObject("Delayed interaction receiver");
        var receiver = receiverObject.AddComponent<InteractionReceiver>();
        var now = 0f;

        try
        {
            SetPrivateValue(fixture.Controller, "timeProvider", new System.Func<float>(() => now));
            receiverObject.transform.position = fixture.Follower.transform.position + Vector3.up * 0.9f;
            receiverObject.AddComponent<BoxCollider>();
            Physics.SyncTransforms();

            fixture.Controller.SetDelay(2f);
            Queue(fixture.Controller, PlayerCommand.Interact(1));
            Queue(fixture.Controller, PlayerCommand.Move(2, Vector2.right));
            Queue(fixture.Controller, PlayerCommand.Interact(3));

            now = 1.99f;
            InvokeUpdate(fixture.Controller);
            Assert.That(receiver.InteractionCount, Is.Zero);
            Assert.That(fixture.Follower.transform.position, Is.EqualTo(fixture.InitialShadowPosition));

            now = 2f;
            InvokeUpdate(fixture.Controller);
            Assert.That(receiver.InteractionCount, Is.EqualTo(2));
            Assert.That(fixture.Follower.transform.position, Is.EqualTo(fixture.InitialShadowPosition));
        }
        finally
        {
            Object.DestroyImmediate(receiverObject);
            DestroyShadowFixture(fixture);
        }
    }

    [Test]
    public void Shorter_reselected_delay_cannot_execute_b_before_long_delay_head_a()
    {
        var fixture = CreateShadowFixture();
        var receiverObject = new GameObject("FIFO head receiver");
        var receiver = receiverObject.AddComponent<InteractionReceiver>();
        var now = 0f;

        try
        {
            SetPrivateValue(fixture.Controller, "timeProvider", new System.Func<float>(() => now));
            receiverObject.transform.position = fixture.Follower.transform.position + Vector3.up * 0.9f;
            receiverObject.AddComponent<BoxCollider>();
            Physics.SyncTransforms();

            fixture.Controller.SetDelay(4f);
            Queue(fixture.Controller, PlayerCommand.Interact(1)); // A
            fixture.Controller.SetDelay(1f);
            Queue(fixture.Controller, PlayerCommand.Interact(2)); // B

            now = 1f;
            InvokeUpdate(fixture.Controller);
            Assert.That(receiver.InteractionCount, Is.Zero, "A is not due yet.");
            Assert.That(GetQueueCount(fixture.Controller, "delayedCommands"), Is.EqualTo(2), "B must not overtake A.");

            now = 4f;
            InvokeUpdate(fixture.Controller);
            Assert.That(receiver.InteractionCount, Is.EqualTo(2), "A and B execute when the FIFO head becomes due.");
            Assert.That(GetQueueCount(fixture.Controller, "delayedCommands"), Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(receiverObject);
            DestroyShadowFixture(fixture);
        }
    }

    [Test]
    public void Shorter_reselected_delay_executes_interactions_in_exact_a_then_b_order()
    {
        var fixture = CreateShadowFixture();
        var aObject = new GameObject("Ordered receiver A");
        var bObject = new GameObject("Ordered receiver B");
        var executionLog = new List<string>();
        var now = 0f;

        try
        {
            SetPrivateValue(fixture.Controller, "timeProvider", new System.Func<float>(() => now));
            var aCollider = aObject.AddComponent<BoxCollider>();
            var bCollider = bObject.AddComponent<BoxCollider>();
            bCollider.enabled = false;
            var receiverA = aObject.AddComponent<OrderedInteractionReceiver>();
            var receiverB = bObject.AddComponent<OrderedInteractionReceiver>();
            receiverA.Configure("A", executionLog, aCollider, bCollider);
            receiverB.Configure("B", executionLog, bCollider, null);
            var interactionPoint = fixture.Follower.transform.position + Vector3.up * 0.9f;
            aObject.transform.position = interactionPoint;
            bObject.transform.position = interactionPoint;
            Physics.SyncTransforms();

            fixture.Controller.SetDelay(4f);
            Queue(fixture.Controller, PlayerCommand.Interact(1));
            fixture.Controller.SetDelay(1f);
            Queue(fixture.Controller, PlayerCommand.Interact(2));

            now = 1f;
            InvokeUpdate(fixture.Controller);
            Assert.That(executionLog, Is.Empty, "The shorter-delay command must not execute before FIFO head A.");

            now = 4f;
            InvokeUpdate(fixture.Controller);
            Assert.That(executionLog, Is.EqualTo(new[] { "A", "B" }));
        }
        finally
        {
            Object.DestroyImmediate(aObject);
            Object.DestroyImmediate(bObject);
            DestroyShadowFixture(fixture);
        }
    }

    private static void SetReference(Object target, string propertyName, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void InvokeAwake(StopwatchController stopwatch)
    {
        typeof(StopwatchController)
            .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(stopwatch, null);
    }

    private static void Queue(ShadowController controller, PlayerCommand command)
    {
        typeof(ShadowController)
            .GetMethod("QueueCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(controller, new object[] { command });
    }

    private static void InvokeUpdate(ShadowController controller)
    {
        typeof(ShadowController)
            .GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(controller, null);
    }

    private static void InvokeFixedUpdate(ShadowController controller)
    {
        typeof(ShadowController)
            .GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(controller, null);
    }

    private static List<object> GetScheduledCommands(ShadowController controller)
    {
        var queue = typeof(ShadowController)
            .GetField("delayedCommands", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(controller);
        var values = (System.Array)queue.GetType().GetMethod("ToArray").Invoke(queue, null);
        var result = new List<object>();
        foreach (var value in values)
        {
            result.Add(value);
        }

        return result;
    }

    private static int GetCommandSequence(object scheduledCommand)
    {
        return ((PlayerCommand)scheduledCommand.GetType().GetProperty("Command").GetValue(scheduledCommand)).Sequence;
    }

    private static Vector2 GetCommandDirection(object scheduledCommand)
    {
        return ((PlayerCommand)scheduledCommand.GetType().GetProperty("Command").GetValue(scheduledCommand)).Direction;
    }

    private static float GetExecuteAt(object scheduledCommand)
    {
        return (float)scheduledCommand.GetType().GetProperty("ExecuteAt").GetValue(scheduledCommand);
    }

    private static float GetPrivateFloat(Object target, string fieldName)
    {
        return (float)target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }

    private static void SetPrivateValue(object target, string fieldName, object value)
    {
        target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }

    private static int GetQueueCount(ShadowController controller, string fieldName)
    {
        var queue = controller.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
        return (int)queue.GetType().GetProperty("Count").GetValue(queue);
    }

    private static ShadowFixture CreateShadowFixture()
    {
        var player = new GameObject("Player");
        var controller = player.AddComponent<ShadowController>();
        var shadowPrefab = new GameObject("Shadow prefab");
        shadowPrefab.AddComponent<CharacterController>();
        shadowPrefab.AddComponent<ShadowFollower>();
        SetReference(controller, "shadowPrefab", shadowPrefab);
        Assert.That(controller.TrySpawn(Vector3.zero), Is.True);
        return new ShadowFixture(player, shadowPrefab, controller, controller.ActiveShadow);
    }

    private static void DestroyShadowFixture(ShadowFixture fixture)
    {
        fixture.Controller.Recall();
        Object.DestroyImmediate(fixture.Player);
        Object.DestroyImmediate(fixture.ShadowPrefab);
    }

    private sealed class StopwatchFeedbackProbe : MonoBehaviour, IStopwatchFeedback
    {
        public bool Unlocked { get; private set; }
        public float LastDelay { get; private set; }
        public bool LastSelecting { get; private set; }
        public bool LastDelayedMode { get; private set; }

        public void SetStopwatchUnlocked(bool unlocked)
        {
            Unlocked = unlocked;
        }

        public void SetStopwatchDial(float delaySeconds, bool selecting, bool delayedMode)
        {
            LastDelay = delaySeconds;
            LastSelecting = selecting;
            LastDelayedMode = delayedMode;
        }
    }

    private sealed class ShadowFixture
    {
        public ShadowFixture(GameObject player, GameObject shadowPrefab, ShadowController controller, ShadowFollower follower)
        {
            Player = player;
            ShadowPrefab = shadowPrefab;
            Controller = controller;
            Follower = follower;
            InitialShadowPosition = follower.transform.position;
        }

        public GameObject Player { get; }
        public GameObject ShadowPrefab { get; }
        public ShadowController Controller { get; }
        public ShadowFollower Follower { get; }
        public Vector3 InitialShadowPosition { get; }
    }

    private sealed class InteractionReceiver : MonoBehaviour, IActorInteractable
    {
        public int InteractionCount { get; private set; }

        public bool TryInteract(ActorKind actor)
        {
            InteractionCount++;
            return true;
        }
    }

    private sealed class OrderedInteractionReceiver : MonoBehaviour, IActorInteractable
    {
        private string marker;
        private List<string> executionLog;
        private Collider ownCollider;
        private Collider nextCollider;

        public void Configure(string markerValue, List<string> log, Collider self, Collider next)
        {
            marker = markerValue;
            executionLog = log;
            ownCollider = self;
            nextCollider = next;
        }

        public bool TryInteract(ActorKind actor)
        {
            executionLog.Add(marker);
            ownCollider.enabled = false;
            if (nextCollider != null)
            {
                nextCollider.enabled = true;
            }

            Physics.SyncTransforms();
            return true;
        }
    }
}
