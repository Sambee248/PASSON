using System;
using System.Collections;
using UnityEngine;

public sealed class StageGate : MonoBehaviour, ICheckpointState
{
    [SerializeField] private string checkpointId;
    [SerializeField] private Collider blockingCollider;
    [SerializeField] private Transform movingGate;
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 3f, 0f);
    [SerializeField, Min(0f)] private float openDurationSeconds = 0.65f;

    private Vector3 closedLocalPosition;
    private bool dependenciesResolved;

    public bool IsOpen { get; private set; }
    public string CheckpointId => string.IsNullOrWhiteSpace(checkpointId)
        ? $"{gameObject.scene.name}/{gameObject.name}" : checkpointId;

    private void Awake()
    {
        ResolveDependencies();
    }

    private void ResolveDependencies()
    {
        if (blockingCollider == null)
        {
            blockingCollider = GetComponent<Collider>();
        }

        if (movingGate == null)
        {
            movingGate = transform;
        }

        if (!dependenciesResolved)
        {
            closedLocalPosition = movingGate.localPosition;
            dependenciesResolved = true;
        }
    }

    public void OpenForTest()
    {
        Open();
    }

    public void Open()
    {
        if (IsOpen)
        {
            return;
        }

        ResolveDependencies();
        IsOpen = true;

        if (blockingCollider != null)
        {
            blockingCollider.enabled = false;
        }

        if (movingGate == null)
        {
            return;
        }

        if (Application.isPlaying && openDurationSeconds > 0f)
        {
            StartCoroutine(AnimateOpen());
        }
        else
        {
            movingGate.localPosition = closedLocalPosition + openOffset;
        }
    }

    public string CaptureState() => JsonUtility.ToJson(new SavedState { open = IsOpen });

    public void RestoreState(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        ResolveDependencies();
        StopAllCoroutines();
        IsOpen = JsonUtility.FromJson<SavedState>(json).open;
        if (blockingCollider != null) blockingCollider.enabled = !IsOpen;
        if (movingGate != null)
            movingGate.localPosition = closedLocalPosition + (IsOpen ? openOffset : Vector3.zero);
        Physics.SyncTransforms();
    }

    private IEnumerator AnimateOpen()
    {
        var start = movingGate.localPosition;
        var end = closedLocalPosition + openOffset;
        var elapsed = 0f;
        while (elapsed < openDurationSeconds)
        {
            elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(elapsed / openDurationSeconds);
            movingGate.localPosition = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }

        movingGate.localPosition = end;
    }

    [Serializable]
    private struct SavedState { public bool open; }
}
