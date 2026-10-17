using NUnit.Framework;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ShadowPlacementTests
{
    [Test]
    public void Point_inside_radius_is_valid_when_surface_is_walkable()
    {
        Assert.That(ShadowPlacement.IsInsideRange(Vector3.zero, new Vector3(4f, 0f, 0f), 5f), Is.True);
        Assert.That(ShadowPlacement.IsInsideRange(Vector3.zero, new Vector3(5.1f, 0f, 0f), 5f), Is.False);
    }

    [Test]
    public void Walkable_floor_does_not_block_capsule_clearance_or_replace_shadow_on_an_invalid_request()
    {
        var fixture = CreateFixture();
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.layer = LayerMask.NameToLayer("Walkable");
        var target = fixture.Player.transform.position + Vector3.right * 4f;
        floor.transform.SetPositionAndRotation(target + Vector3.down * 0.5f, Quaternion.identity);
        floor.transform.localScale = new Vector3(4f, 1f, 4f);
        Physics.SyncTransforms();
        var board = ConfigureSpawnCell(fixture, floor.GetComponent<Collider>());

        try
        {
            Assert.That(fixture.Placement.TryPlace(target, floor.GetComponent<Collider>()), Is.True);
            Assert.That(fixture.Controller.HasShadow, Is.True);

            Assert.That(fixture.Placement.TryPlace(fixture.Player.transform.position + Vector3.right * 12f), Is.False);
            Assert.That(fixture.Controller.HasShadow, Is.True);
        }
        finally
        {
            DestroyFixture(fixture, floor, board);
        }
    }

    [Test]
    public void Walkable_wall_blocks_placement_and_retains_the_existing_shadow()
    {
        var fixture = CreateFixture();
        var existingPoint = fixture.Player.transform.position + Vector3.left * 3f;
        var wallPoint = fixture.Player.transform.position + Vector3.right * 4f;
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.layer = LayerMask.NameToLayer("Walkable");
        wall.transform.SetPositionAndRotation(wallPoint + Vector3.up, Quaternion.identity);
        wall.transform.localScale = new Vector3(1f, 2f, 1f);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.layer = LayerMask.NameToLayer("Walkable");
        floor.transform.SetPositionAndRotation(wallPoint + Vector3.down * 0.5f, Quaternion.identity);
        floor.transform.localScale = new Vector3(4f, 1f, 4f);
        Physics.SyncTransforms();
        var board = ConfigureSpawnCell(fixture, floor.GetComponent<Collider>());

        try
        {
            Assert.That(fixture.Controller.TrySpawn(existingPoint), Is.True);
            Assert.That(fixture.Placement.TryPlace(wallPoint, floor.GetComponent<Collider>()), Is.False);
            Assert.That(fixture.Controller.HasShadow, Is.True);
        }
        finally
        {
            DestroyFixture(fixture, wall, floor, board);
        }
    }

    [Test]
    public void Distinct_flush_walkable_obstruction_blocks_placement_even_when_the_floor_is_selected_support()
    {
        var fixture = CreateFixture();
        var existingPoint = fixture.Player.transform.position + Vector3.left * 3f;
        var target = fixture.Player.transform.position + Vector3.right * 4f;
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.layer = LayerMask.NameToLayer("Walkable");
        floor.transform.SetPositionAndRotation(target + Vector3.down * 0.5f, Quaternion.identity);
        floor.transform.localScale = new Vector3(4f, 1f, 4f);
        var obstruction = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstruction.layer = LayerMask.NameToLayer("Walkable");
        obstruction.transform.SetPositionAndRotation(target + Vector3.up * 0.0025f, Quaternion.identity);
        obstruction.transform.localScale = new Vector3(0.5f, 0.005f, 0.5f);
        Physics.SyncTransforms();
        var board = ConfigureSpawnCell(fixture, floor.GetComponent<Collider>());

        try
        {
            Assert.That(fixture.Controller.TrySpawn(existingPoint), Is.True);
            Assert.That(fixture.Placement.TryPlace(target, floor.GetComponent<Collider>()), Is.False);
            Assert.That(fixture.Controller.HasShadow, Is.True);
        }
        finally
        {
            DestroyFixture(fixture, floor, obstruction, board);
        }
    }

    [Test]
    public void Unregistered_floor_cannot_place_a_shadow()
    {
        var fixture = CreateFixture();
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.layer = LayerMask.NameToLayer("Walkable");
        var point = fixture.Player.transform.position + Vector3.right * 4f;
        floor.transform.SetPositionAndRotation(point + Vector3.down * 0.5f, Quaternion.identity);
        floor.transform.localScale = new Vector3(4f, 1f, 4f);
        Physics.SyncTransforms();
        try
        {
            Assert.That(fixture.Placement.TryPlace(point, floor.GetComponent<Collider>()), Is.False);
            Assert.That(fixture.Controller.HasShadow, Is.False);
        }
        finally
        {
            DestroyFixture(fixture, floor);
        }
    }

    [UnityTest]
    public IEnumerator Shadow_emerges_from_the_required_offset_at_the_walkable_point()
    {
        var fixture = CreateFixture(emergenceDuration: 0f);
        try
        {
            var target = fixture.Player.transform.position + Vector3.right * 3f;
            Assert.That(ShadowController.GetEmergenceStart(target, 1.2f), Is.EqualTo(target - Vector3.up * 1.2f));
            Assert.That(fixture.Controller.TrySpawn(target), Is.True);
            var follower = fixture.Controller.ActiveShadow;

            yield return null;

            Assert.That(follower.transform.position, Is.EqualTo(target));
            Assert.That(follower.enabled, Is.True);
        }
        finally
        {
            DestroyFixture(fixture);
        }
    }

    [Test]
    public void Fixed_update_ignores_queued_movement_but_routes_interaction_to_the_shadow()
    {
        var fixture = CreateFixture();
        try
        {
            Assert.That(fixture.Controller.TrySpawn(Vector3.zero), Is.True);
            var follower = fixture.Controller.ActiveShadow;
            var positionBeforeCommands = follower.transform.position;
            var interactableObject = new GameObject("Interactable");
            interactableObject.transform.position = follower.transform.position + Vector3.up * 0.9f;
            interactableObject.AddComponent<BoxCollider>();
            var interactable = interactableObject.AddComponent<ShadowInteractable>();
            Physics.SyncTransforms();

            Queue(fixture.Controller, PlayerCommand.Move(1, Vector2.right));
            Queue(fixture.Controller, PlayerCommand.Interact(2));
            InvokeFixedUpdate(fixture.Controller);

            Assert.That(follower.transform.position, Is.EqualTo(positionBeforeCommands));
            Assert.That(interactable.ReceivedActor, Is.EqualTo(ActorKind.Shadow));
            Object.DestroyImmediate(interactableObject);
        }
        finally
        {
            DestroyFixture(fixture);
        }
    }

    [Test]
    public void Player_and_shadow_prefabs_use_required_layers_and_collision_setup()
    {
        var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        var shadow = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Shadow.prefab");
        var playerLayer = LayerMask.NameToLayer("Player");
        var shadowLayer = LayerMask.NameToLayer("Shadow");
        var enemyLayer = LayerMask.NameToLayer("Enemy");

        Assert.That(player.tag, Is.EqualTo("Player"));
        Assert.That(player.layer, Is.EqualTo(playerLayer));
        Assert.That(shadow.layer, Is.EqualTo(shadowLayer));
        Assert.That(shadow.GetComponent<ShadowFollower>(), Is.Not.Null);
        Assert.That(shadow.GetComponent<PathMover>(), Is.Null);
        Assert.That(Physics.GetIgnoreLayerCollision(shadowLayer, playerLayer), Is.True);
        Assert.That(Physics.GetIgnoreLayerCollision(shadowLayer, enemyLayer), Is.True);
    }

    private static Fixture CreateFixture(float emergenceDuration = 0.35f)
    {
        var player = new GameObject("Player");
        player.transform.position = new Vector3(1000f, 0f, 1000f);
        player.AddComponent<CharacterController>();
        player.AddComponent<PathMover>();
        var source = player.AddComponent<PlayerCommandSource>();
        var controller = player.AddComponent<ShadowController>();
        var placement = player.AddComponent<ShadowPlacement>();

        var shadowPrefab = new GameObject("ShadowPrefab");
        shadowPrefab.AddComponent<CharacterController>();
        shadowPrefab.AddComponent<ShadowFollower>();

        SetReference(controller, "shadowPrefab", shadowPrefab);
        SetReference(controller, "playerCommandSource", source);
        SetFloat(controller, "emergenceDuration", emergenceDuration);
        SetReference(placement, "shadowController", controller);
        SetReference(placement, "playerTransform", player.transform);
        SetMask(placement, "walkableMask", LayerMask.GetMask("Walkable"));
        SetMask(placement, "clearanceMask", ~0);
        return new Fixture(player, shadowPrefab, controller, placement);
    }

    private static GameObject ConfigureSpawnCell(Fixture fixture, Collider surface)
    {
        var cell = surface.gameObject.AddComponent<GridCell>();
        cell.Configure(Vector2Int.zero, false, true, true, surface, null, null, true);
        var boardRoot = new GameObject("PlacementTestBoard");
        var board = boardRoot.AddComponent<GridBoard>();
        board.Configure(4f, new[] { cell });
        fixture.Placement.Configure(board, fixture.Controller);
        return boardRoot;
    }

    private static void DestroyFixture(Fixture fixture, params GameObject[] extras)
    {
        fixture.Controller.Recall();
        foreach (var extra in extras)
        {
            Object.DestroyImmediate(extra);
        }

        Object.DestroyImmediate(fixture.Player);
        Object.DestroyImmediate(fixture.ShadowPrefab);
    }

    private static void Queue(ShadowController controller, PlayerCommand command)
    {
        typeof(ShadowController)
            .GetMethod("QueueCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(controller, new object[] { command });
    }

    private static void InvokeFixedUpdate(ShadowController controller)
    {
        typeof(ShadowController)
            .GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(controller, null);
    }

    private static void SetReference(Object target, string propertyName, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string propertyName, float value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetMask(Object target, string propertyName, int value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private sealed class Fixture
    {
        public Fixture(GameObject player, GameObject shadowPrefab, ShadowController controller, ShadowPlacement placement)
        {
            Player = player;
            ShadowPrefab = shadowPrefab;
            Controller = controller;
            Placement = placement;
        }

        public GameObject Player { get; }
        public GameObject ShadowPrefab { get; }
        public ShadowController Controller { get; }
        public ShadowPlacement Placement { get; }
    }

    private sealed class ShadowInteractable : MonoBehaviour, IActorInteractable
    {
        public ActorKind ReceivedActor { get; private set; }

        public bool TryInteract(ActorKind actor)
        {
            ReceivedActor = actor;
            return true;
        }
    }
}
