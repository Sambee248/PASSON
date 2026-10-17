using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ShadowPlacementValidatorTests
{
    private readonly List<GameObject> created = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (var item in created)
        {
            if (item != null) Object.DestroyImmediate(item);
        }
        created.Clear();
    }

    [Test]
    public void Only_supported_clear_spawn_tiles_within_range_are_listed()
    {
        var player = Actor();
        var valid = Tile(0, 4f, true);
        var plate = Tile(1, 5.5f, false);
        var far = Tile(2, 12f, true);
        var board = Board(valid, plate, far);
        var validator = Validator(board, player.transform);
        Physics.SyncTransforms();

        Assert.That(validator.IsEligible(valid), Is.True);
        Assert.That(validator.IsEligible(plate), Is.False);
        Assert.That(validator.IsEligible(far), Is.False);
        Assert.That(validator.GetEligibleCells(), Is.EquivalentTo(new[] { valid }));

        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        created.Add(wall);
        wall.transform.position = valid.SurfacePoint + Vector3.up * 0.9f;
        wall.transform.localScale = new Vector3(0.5f, 1.8f, 0.5f);
        Physics.SyncTransforms();
        Assert.That(validator.IsEligible(valid), Is.False);
        Assert.That(validator.GetEligibleCells(), Is.Empty);
    }

    [Test]
    public void Cursor_resolves_only_real_board_surface_and_invalid_tile_preserves_shadow()
    {
        var player = Actor();
        var valid = Tile(0, 4f, true);
        var plate = Tile(1, 5.5f, false);
        var board = Board(valid, plate);
        var validator = Validator(board, player.transform);
        var placement = player.AddComponent<ShadowPlacement>();
        var controller = player.AddComponent<ShadowController>();
        var prefab = new GameObject("ShadowPrefab");
        created.Add(prefab);
        prefab.AddComponent<CharacterController>();
        prefab.AddComponent<ShadowFollower>();
        SetReference(controller, "shadowPrefab", prefab);
        SetReference(placement, "shadowController", controller);
        SetReference(placement, "board", board);
        SetReference(placement, "playerTransform", player.transform);
        SetMask(placement, "walkableMask", LayerMask.GetMask("Walkable"));
        var decorative = GameObject.CreatePrimitive(PrimitiveType.Cube);
        created.Add(decorative);
        decorative.layer = LayerMask.NameToLayer("Walkable");
        decorative.transform.position = new Vector3(1007f, 0.9f, 1000f);
        decorative.transform.localScale = new Vector3(1.5f, 0.2f, 1.5f);
        Physics.SyncTransforms();

        try
        {
            Assert.That(validator.TryResolveCursor(
                new Ray(valid.SurfacePoint + Vector3.up * 4f, Vector3.down), out var selected), Is.True);
            Assert.That(selected, Is.SameAs(valid));
            Assert.That(validator.TryResolveCursor(
                new Ray(decorative.transform.position + Vector3.up * 4f, Vector3.down), out _), Is.False);
            Assert.That(controller.TrySpawn(new Vector3(1000f, 1f, 1000f)), Is.True);
            var previous = controller.ActiveShadow;
            Assert.That(validator.IsEligible(valid), Is.True,
                "A shadow at the player start must not block a distant spawn tile.");
            Assert.That(placement.TryPlace(plate), Is.False);
            Assert.That(controller.ActiveShadow, Is.SameAs(previous));
            Assert.That(validator.IsEligible(valid), Is.True,
                "A rejected plate tile must not invalidate a different spawn tile.");
            Assert.That(placement.TryPlace(valid), Is.True);
            Assert.That(controller.ActiveShadow, Is.Not.SameAs(previous));
        }
        finally
        {
            controller.Recall();
        }
    }

    private GameObject Actor()
    {
        var actor = new GameObject("Player");
        created.Add(actor);
        actor.transform.position = new Vector3(1000f, 1f, 1000f);
        actor.AddComponent<CharacterController>();
        return actor;
    }

    private GridCell Tile(int x, float offset, bool spawnable)
    {
        var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        created.Add(tile);
        tile.layer = LayerMask.NameToLayer("Walkable");
        tile.transform.position = new Vector3(1000f + offset, 0.9f, 1000f);
        tile.transform.localScale = new Vector3(1.5f, 0.2f, 1.5f);
        var cell = tile.AddComponent<GridCell>();
        cell.Configure(new Vector2Int(x, 0), false, true, spawnable,
            tile.GetComponent<Collider>(), null, null, true);
        return cell;
    }

    private GridBoard Board(params GridCell[] cells)
    {
        var root = new GameObject("GridBoard");
        created.Add(root);
        var board = root.AddComponent<GridBoard>();
        board.Configure(1.5f, cells);
        return board;
    }

    private static ShadowPlacementValidator Validator(GridBoard board, Transform player) =>
        new ShadowPlacementValidator(board, player, 8f, 0.35f, 1.8f,
            ~0, LayerMask.GetMask("Walkable"), 100f);

    private static void SetReference(Object target, string propertyName, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetMask(Object target, string propertyName, int value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
