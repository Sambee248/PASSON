using NUnit.Framework;
using UnityEngine;

public sealed class PlayerInteractorTests
{
    [Test]
    public void E_reaches_only_nearby_interactables_while_the_player_is_idle()
    {
        var player = new GameObject("player");
        player.tag = "Player";
        player.AddComponent<CharacterController>();
        var path = player.AddComponent<PlayerPathController>();
        var commands = player.AddComponent<PlayerCommandSource>();
        var interactor = player.AddComponent<PlayerInteractor>();
        interactor.Configure(commands, path, 1.8f);

        var root = new GameObject("turntable");
        root.transform.position = Vector3.right;
        root.AddComponent<BoxCollider>();
        var rotor = new GameObject("rotor").transform;
        rotor.SetParent(root.transform);
        var module = root.AddComponent<RotationModule>();
        module.Configure(rotor, null, new[] { 0f, 90f });
        Physics.SyncTransforms();

        commands.PublishInteractForTest();
        Assert.That(module.CurrentIndex, Is.EqualTo(1));

        root.transform.position = Vector3.right * 5f;
        Physics.SyncTransforms();
        commands.PublishInteractForTest();
        Assert.That(module.CurrentIndex, Is.EqualTo(1), "Out-of-reach mechanisms must not rotate.");

        Object.DestroyImmediate(root);
        Object.DestroyImmediate(player);
    }
}
