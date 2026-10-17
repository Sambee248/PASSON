using UnityEngine;

[DisallowMultipleComponent]
public sealed class GridCell : MonoBehaviour
{
    [SerializeField] private Vector2Int coordinate;
    [SerializeField] private bool playerWalkable;
    [SerializeField] private bool shadowWalkable;
    [SerializeField] private bool shadowSpawnable;
    [SerializeField] private Collider surface;
    [SerializeField] private PathNode node;
    [SerializeField] private Collider playerAccessCondition;
    [SerializeField] private bool requiredConditionEnabled = true;

    public Vector2Int Coordinate => coordinate;
    public bool PlayerAccessible => playerWalkable && SurfaceReady && node != null
        && (playerAccessCondition == null || playerAccessCondition.enabled == requiredConditionEnabled);
    public bool ShadowWalkable => shadowWalkable && SurfaceReady;
    public bool ShadowSpawnable => shadowSpawnable && ShadowWalkable;
    public Collider Surface => surface;
    public PathNode Node => node;
    public Vector3 SurfacePoint => surface != null
        ? new Vector3(surface.bounds.center.x, surface.bounds.max.y, surface.bounds.center.z)
        : transform.position;

    private bool SurfaceReady => isActiveAndEnabled && surface != null && surface.enabled
        && surface.gameObject.activeInHierarchy;

    public void Configure(Vector2Int cellCoordinate, bool allowPlayer, bool allowShadow,
        bool allowSpawn, Collider surfaceCollider, PathNode pathNode,
        Collider accessCondition, bool conditionEnabled)
    {
        coordinate = cellCoordinate;
        playerWalkable = allowPlayer;
        shadowWalkable = allowShadow;
        shadowSpawnable = allowSpawn;
        surface = surfaceCollider;
        node = pathNode;
        playerAccessCondition = accessCondition;
        requiredConditionEnabled = conditionEnabled;
    }
}
