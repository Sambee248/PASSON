using UnityEngine;

public sealed class ShadowPlacement : MonoBehaviour
{
    [SerializeField] private ShadowController shadowController;
    [SerializeField] private GridBoard board;
    [SerializeField] private PlayerPathController playerPathController;
    [SerializeField] private ShadowPlacementPreview preview;
    [SerializeField] private Camera placementCamera;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private LayerMask walkableMask;
    [SerializeField] private LayerMask clearanceMask = ~0;
    [SerializeField] private float placementRadius = 8f;
    [SerializeField] private float cursorRayDistance = 100f;
    [SerializeField] private float capsuleRadius = 0.35f;
    [SerializeField] private float capsuleHeight = 1.8f;

    private ShadowPlacementValidator validator;
    private bool previewActive;

    public ShadowPlacementPreview Preview => EnsurePreview();

    private void Awake()
    {
        if (shadowController == null)
        {
            shadowController = GetComponent<ShadowController>();
        }
        if (playerPathController == null) playerPathController = GetComponent<PlayerPathController>();

        if (playerTransform == null)
        {
            playerTransform = transform;
        }

        if (walkableMask.value == 0)
        {
            walkableMask = LayerMask.GetMask("Walkable");
        }
        if (board == null)
        {
            board = FindFirstObjectByType<GridBoard>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F)) BeginPreview();
        if (previewActive && Input.GetKey(KeyCode.F))
        {
            var cameraToUse = placementCamera != null ? placementCamera : Camera.main;
            if (cameraToUse != null) UpdatePreview(cameraToUse.ScreenPointToRay(Input.mousePosition));
        }
        if (Input.GetKeyUp(KeyCode.F)) EndPreview();
    }

    private void OnDisable()
    {
        previewActive = false;
        if (preview != null) preview.Hide();
    }

    public void Configure(GridBoard gridBoard, ShadowController controller)
    {
        board = gridBoard;
        shadowController = controller;
        if (playerTransform == null) playerTransform = transform;
        if (playerPathController == null) playerPathController = GetComponent<PlayerPathController>();
        if (walkableMask.value == 0) walkableMask = LayerMask.GetMask("Walkable");
        validator = null;
    }

    public bool BeginPreview()
    {
        if (PlayerIsMoving || ResolveValidator() == null)
        {
            previewActive = false;
            EnsurePreview().Hide();
            return false;
        }
        previewActive = true;
        EnsurePreview().Show(validator.GetEligibleCells(), null);
        return true;
    }

    public void UpdatePreview(Ray cursorRay)
    {
        if (!previewActive) return;
        if (PlayerIsMoving)
        {
            previewActive = false;
            EnsurePreview().Hide();
            return;
        }
        var eligible = ResolveValidator().GetEligibleCells();
        validator.TryResolveCursor(cursorRay, out var hovered);
        EnsurePreview().Show(eligible, hovered);
    }

    public bool EndPreview()
    {
        var selected = previewActive && !PlayerIsMoving && preview != null
            ? preview.HoveredCell : null;
        previewActive = false;
        if (preview != null) preview.Hide();
        return selected != null && TryPlace(selected);
    }

    public bool TryPlace(Vector3 point)
    {
        return TryPlace(point, null);
    }

    public bool TryPlace(Vector3 point, Collider supportCollider)
    {
        if (shadowController == null || playerTransform == null)
        {
            return false;
        }

        if (board == null) return false;
        GridCell cell;
        if (supportCollider != null)
        {
            return board.TryGetCell(supportCollider, out cell) && TryPlace(cell);
        }
        return board.TryGetCellAtWorld(point, out cell) && TryPlace(cell);
    }

    public bool TryPlace(GridCell cell)
    {
        return !PlayerIsMoving && shadowController != null && ResolveValidator() != null
            && validator.IsEligible(cell) && shadowController.TrySpawn(cell.SurfacePoint);
    }

    private bool PlayerIsMoving => playerPathController != null && playerPathController.IsMoving;

    private ShadowPlacementPreview EnsurePreview()
    {
        if (preview == null)
        {
            var root = new GameObject("ShadowPlacementPreview");
            root.transform.SetParent(transform, false);
            preview = root.AddComponent<ShadowPlacementPreview>();
        }
        return preview;
    }

    public static bool IsInsideRange(Vector3 origin, Vector3 point, float radius)
    {
        return Vector3.Distance(origin, point) <= radius;
    }

    private ShadowPlacementValidator ResolveValidator()
    {
        if (board == null) return null;
        if (validator == null)
        {
            validator = new ShadowPlacementValidator(board, playerTransform, placementRadius,
                capsuleRadius, capsuleHeight, clearanceMask, walkableMask, cursorRayDistance);
        }
        return validator;
    }

}
