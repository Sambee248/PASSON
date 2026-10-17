using System;
using System.Collections;
using UnityEngine;

public sealed class AngleSnapCamera : MonoBehaviour
{
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform[] angleAnchors = Array.Empty<Transform>();
    [SerializeField, Min(0f)] private float snapToleranceDegrees = 3f;
    [SerializeField, Min(0f)] private float snapDurationSeconds = 0.18f;
    [SerializeField] private Transform followActor;
    [SerializeField, Range(0f, 1f)] private float followStrength = 0.7f;
    [SerializeField] private Vector2 followXLimits = new Vector2(-8f, 14f);
    [SerializeField, Min(0f)] private float followSmoothTime = 0.3f;

    private Coroutine snapRoutine;
    private int selectedAnchorIndex = -1;
    private Vector3 followVelocity;

    public bool IsLocked { get; private set; }
    public int LockedAngleId { get; private set; } = -1;

    public static KeyCode PreviousAngleKey => KeyCode.Z;
    public static KeyCode NextAngleKey => KeyCode.X;

    public event Action<int> AngleLocked;

    private void Update()
    {
        if (Input.GetKeyDown(PreviousAngleKey))
        {
            SelectRelativeAnchor(-1);
        }
        else if (Input.GetKeyDown(NextAngleKey))
        {
            SelectRelativeAnchor(1);
        }
    }

    private void LateUpdate()
    {
        if (snapRoutine != null) return;
        if (IsLocked && selectedAnchorIndex >= 0 && selectedAnchorIndex < angleAnchors.Length
            && angleAnchors[selectedAnchorIndex] != null && followActor != null)
        {
            transform.position = Vector3.SmoothDamp(transform.position,
                FollowPosition(angleAnchors[selectedAnchorIndex]), ref followVelocity,
                followSmoothTime);
        }
        else if (cameraTarget != null && !IsLocked)
        {
            transform.LookAt(cameraTarget.position, Vector3.up);
            TryPassiveSnapToNearestAnchor();
        }
    }

    private void OnDisable()
    {
        if (snapRoutine != null)
        {
            StopCoroutine(snapRoutine);
            snapRoutine = null;
        }
    }

    public static bool IsWithinTolerance(float targetAngle, float anchorAngle, float toleranceDegrees)
    {
        return Mathf.Abs(Mathf.DeltaAngle(targetAngle, anchorAngle)) <= Mathf.Max(0f, toleranceDegrees);
    }

    public void SetAnchorIndexForTest(int index)
    {
        SelectAnchor(index);
    }

    public void ConfigureAnchorsForTest(params Transform[] anchors)
    {
        angleAnchors = anchors ?? Array.Empty<Transform>();
    }

    public void ConfigureScene(Transform target, params Transform[] anchors)
    {
        cameraTarget = target;
        angleAnchors = anchors ?? Array.Empty<Transform>();
    }

    public void SetSnapDurationForTest(float durationSeconds)
    {
        snapDurationSeconds = Mathf.Max(0f, durationSeconds);
    }

    public void ConfigureFollow(Transform actor, float strength, Vector2 xLimits, float smoothTime)
    {
        followActor = actor;
        followStrength = Mathf.Clamp01(strength);
        followXLimits = new Vector2(Mathf.Min(xLimits.x, xLimits.y),
            Mathf.Max(xLimits.x, xLimits.y));
        followSmoothTime = Mathf.Max(0f, smoothTime);
        followVelocity = Vector3.zero;
    }

    private void SelectRelativeAnchor(int direction)
    {
        if (angleAnchors == null || angleAnchors.Length == 0)
        {
            return;
        }

        var currentIndex = selectedAnchorIndex;
        if (currentIndex < 0 || currentIndex >= angleAnchors.Length)
        {
            currentIndex = FindNearestAnchorIndex();
        }

        SelectAnchor((currentIndex + direction + angleAnchors.Length) % angleAnchors.Length);
    }

    private void SelectAnchor(int index)
    {
        if (angleAnchors == null || index < 0 || index >= angleAnchors.Length || angleAnchors[index] == null)
        {
            return;
        }

        selectedAnchorIndex = index;
        ClearLock();

        StartSnap(index);
    }

    private void TryPassiveSnapToNearestAnchor()
    {
        if (angleAnchors == null || angleAnchors.Length == 0)
        {
            return;
        }

        var index = FindNearestAnchorIndex();
        var anchor = angleAnchors[index];
        if (anchor == null
            || !IsWithinTolerance(transform.eulerAngles.y, anchor.eulerAngles.y, snapToleranceDegrees))
        {
            return;
        }

        selectedAnchorIndex = index;
        StartSnap(index);
    }

    private void StartSnap(int index)
    {
        if (snapRoutine != null)
        {
            StopCoroutine(snapRoutine);
        }

        snapRoutine = StartCoroutine(SnapToAnchor(index));
    }

    private IEnumerator SnapToAnchor(int index)
    {
        var anchor = angleAnchors[index];
        var startPosition = transform.position;
        var startRotation = transform.rotation;
        var elapsed = 0f;

        while (elapsed < snapDurationSeconds)
        {
            elapsed += Time.deltaTime;
            var progress = snapDurationSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / snapDurationSeconds);
            var easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            transform.SetPositionAndRotation(
                Vector3.Lerp(startPosition, FollowPosition(anchor), easedProgress),
                Quaternion.Slerp(startRotation, anchor.rotation, easedProgress));
            yield return null;
        }

        transform.SetPositionAndRotation(FollowPosition(anchor), anchor.rotation);
        followVelocity = Vector3.zero;
        snapRoutine = null;
        IsLocked = true;
        LockedAngleId = index;
        AngleLocked?.Invoke(index);
    }

    private int FindNearestAnchorIndex()
    {
        var nearestIndex = 0;
        var nearestAngle = float.MaxValue;

        for (var index = 0; index < angleAnchors.Length; index++)
        {
            var anchor = angleAnchors[index];
            if (anchor == null)
            {
                continue;
            }

            var angle = Quaternion.Angle(transform.rotation, anchor.rotation);
            if (angle < nearestAngle)
            {
                nearestAngle = angle;
                nearestIndex = index;
            }
        }

        return nearestIndex;
    }

    private void ClearLock()
    {
        IsLocked = false;
        LockedAngleId = -1;
    }

    private Vector3 FollowPosition(Transform anchor)
    {
        if (followActor == null) return anchor.position;
        var offset = Mathf.Clamp(followActor.position.x, followXLimits.x, followXLimits.y)
            * followStrength;
        return anchor.position + Vector3.right * offset;
    }
}
