using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public sealed class ShadowFollower : MonoBehaviour
{
    [SerializeField] private GridBoard board;
    [SerializeField] private float turnSpeed = 12f;
    [SerializeField] private float killHeight = -5f;

    private CharacterController capsule;

    public event Action Fell;

    private void Awake()
    {
        capsule = GetComponent<CharacterController>();
        capsule.minMoveDistance = 0f;
        if (board == null) board = FindFirstObjectByType<GridBoard>();
    }

    private void Update()
    {
        if (transform.position.y < killHeight) Fell?.Invoke();
    }

    public void Configure(GridBoard gridBoard) => board = gridBoard;

    public Vector3 ApplyDisplacement(Vector3 requested)
    {
        if (!isActiveAndEnabled || !capsule.enabled) return Vector3.zero;
        var horizontal = Vector3.ProjectOnPlane(requested, Vector3.up);
        var length = horizontal.magnitude;
        if (length < 0.0001f) return Vector3.zero;

        var beforeAll = transform.position;
        var stepLimit = Mathf.Max(0.01f, Mathf.Min(
            board != null ? board.CellPitch * 0.25f : capsule.radius * 0.5f,
            capsule.radius * 0.5f));
        var direction = horizontal / length;
        var remaining = length;
        while (remaining > 0.0001f)
        {
            var lengthThisStep = Mathf.Min(stepLimit, remaining);
            var step = direction * lengthThisStep;
            var beforeStep = transform.position;
            var supportedLength = lengthThisStep;
            if (board != null && !board.CanShadowOccupy(beforeStep + step, capsule.radius))
            {
                // Clip at the last valid footprint, rather than skipping an empty tile.
                var low = 0f;
                var high = lengthThisStep;
                for (var iteration = 0; iteration < 8; iteration++)
                {
                    var mid = (low + high) * 0.5f;
                    if (board.CanShadowOccupy(beforeStep + direction * mid, capsule.radius)) low = mid;
                    else high = mid;
                }
                supportedLength = low;
            }

            if (supportedLength > 0.0001f)
            {
                capsule.Move(direction * supportedLength);
            }
            var actual = Vector3.ProjectOnPlane(transform.position - beforeStep, Vector3.up);
            // Neither a missing tile nor a wall can create a deferred motion debt.
            if (supportedLength < lengthThisStep - 0.001f
                || actual.magnitude < supportedLength - 0.01f)
            {
                break;
            }
            remaining -= lengthThisStep;
        }

        var achieved = Vector3.ProjectOnPlane(transform.position - beforeAll, Vector3.up);
        if (achieved.sqrMagnitude > 0.0001f)
        {
            var facing = Quaternion.LookRotation(achieved.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, facing,
                1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
        }
        return achieved;
    }
}
