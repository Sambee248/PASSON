using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public sealed class LockableBridge : MonoBehaviour
{
    [SerializeField] private AngleSnapCamera angleSnapCamera;
    [SerializeField] private int matchingAngleId;
    [SerializeField] private BoxCollider bridgeCollider;
    [SerializeField] private Renderer[] bridgeRenderers;
    [SerializeField] private LayerMask actorOccupancyMask;

    private bool disablePending;

    private void Awake()
    {
        if (bridgeCollider == null)
        {
            bridgeCollider = GetComponent<BoxCollider>();
        }

        if (bridgeRenderers == null || bridgeRenderers.Length == 0)
        {
            bridgeRenderers = GetComponentsInChildren<Renderer>(true);
        }

        if (actorOccupancyMask.value == 0)
        {
            actorOccupancyMask = LayerMask.GetMask("Player", "Shadow");
        }

        SetBridgeActive(false);
    }

    private void OnEnable()
    {
        if (angleSnapCamera != null)
        {
            angleSnapCamera.AngleLocked += HandleAngleLocked;
        }
    }

    private void Start()
    {
        SynchronizeWithCamera();
    }

    private void FixedUpdate()
    {
        SynchronizeWithCamera();

        if (disablePending && !IsOccupiedByActor())
        {
            disablePending = false;
            SetBridgeActive(false);
        }
    }

    private void OnDisable()
    {
        if (angleSnapCamera != null)
        {
            angleSnapCamera.AngleLocked -= HandleAngleLocked;
        }
    }

    private void HandleAngleLocked(int angleId)
    {
        RequestState(angleId == matchingAngleId);
    }

    private void SynchronizeWithCamera()
    {
        var shouldBeActive = angleSnapCamera != null
            && angleSnapCamera.IsLocked
            && angleSnapCamera.LockedAngleId == matchingAngleId;

        RequestState(shouldBeActive);
    }

    private void RequestState(bool shouldBeActive)
    {
        if (shouldBeActive)
        {
            disablePending = false;
            SetBridgeActive(true);
            return;
        }

        if (!IsBridgeActive())
        {
            disablePending = false;
            return;
        }

        if (IsOccupiedByActor())
        {
            disablePending = true;
            return;
        }

        disablePending = false;
        SetBridgeActive(false);
    }

    private bool IsOccupiedByActor()
    {
        if (bridgeCollider == null || actorOccupancyMask.value == 0)
        {
            return false;
        }

        var center = transform.TransformPoint(bridgeCollider.center);
        var halfExtents = Vector3.Scale(bridgeCollider.size * 0.5f, transform.lossyScale);
        return Physics.CheckBox(
            center,
            halfExtents,
            transform.rotation,
            actorOccupancyMask,
            QueryTriggerInteraction.Ignore);
    }

    private bool IsBridgeActive()
    {
        return bridgeCollider != null && bridgeCollider.enabled;
    }

    private void SetBridgeActive(bool isActive)
    {
        if (bridgeCollider != null)
        {
            bridgeCollider.enabled = isActive;
        }

        foreach (var bridgeRenderer in bridgeRenderers)
        {
            if (bridgeRenderer != null)
            {
                bridgeRenderer.enabled = isActive;
            }
        }
    }
}
