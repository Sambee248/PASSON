using NUnit.Framework;
using UnityEngine;

public sealed class Stage02MechanicsTests
{
    [Test]
    public void Rotation_is_player_only_checkpointable_and_refreshes_graph()
    {
        var root = new GameObject("rotation-test");
        var graph = root.AddComponent<PathGraph>();
        var rotor = new GameObject("rotor").transform;
        rotor.SetParent(root.transform);
        var module = root.AddComponent<RotationModule>();
        module.Configure(rotor, graph, new[] { 0f, 90f, 180f, 270f });
        var beforeVersion = graph.Version;

        Assert.That(module.TryInteract(ActorKind.Shadow), Is.False);
        Assert.That(module.CurrentIndex, Is.EqualTo(0));
        Assert.That(module.TryInteract(ActorKind.Player), Is.True);
        Assert.That(module.CurrentIndex, Is.EqualTo(1));
        Assert.That(Mathf.DeltaAngle(rotor.localEulerAngles.y, 90f), Is.EqualTo(0f).Within(0.01f));
        Assert.That(graph.Version, Is.GreaterThan(beforeVersion));

        var saved = module.CaptureState();
        module.RotateNext();
        module.RestoreState(saved);
        Assert.That(module.CurrentIndex, Is.EqualTo(1));
        Assert.That(Mathf.DeltaAngle(rotor.localEulerAngles.y, 90f), Is.EqualTo(0f).Within(0.01f));
        Object.DestroyImmediate(root);
    }

    [Test]
    public void Hazard_clears_enemy_permanently_after_a_successful_hit()
    {
        var enemyObject = new GameObject("patrol");
        var collider = enemyObject.AddComponent<BoxCollider>();
        var enemy = enemyObject.AddComponent<PatrolEnemy>();
        var hazardObject = new GameObject("hazard");
        var hazard = hazardObject.AddComponent<EnvironmentalHazard>();

        hazard.TriggerForTest(enemy);

        Assert.That(enemy.IsCleared, Is.True);
        Assert.That(collider.enabled, Is.False);
        Assert.That(hazard.IsSpent, Is.True);
        Assert.That(hazard.TryInteract(ActorKind.Player), Is.False);
        Object.DestroyImmediate(hazardObject);
        Object.DestroyImmediate(enemyObject);
    }

    [Test]
    public void A_missed_hazard_does_not_spend_it_or_clear_a_later_enemy()
    {
        var hazardObject = new GameObject("hazard");
        var hazard = hazardObject.AddComponent<EnvironmentalHazard>();
        hazard.TriggerForTest(null);
        Assert.That(hazard.IsSpent, Is.False);
        Object.DestroyImmediate(hazardObject);
    }
}
