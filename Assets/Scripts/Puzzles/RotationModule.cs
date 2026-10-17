using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RotationModule : MonoBehaviour, IActorInteractable, ICheckpointState
{
    [SerializeField] private string checkpointId;
    [SerializeField] private Transform rotatingRoot;
    [SerializeField] private PathGraph graph;
    [SerializeField] private float[] yawAngles = { 0f, 90f, 180f, 270f };
    [SerializeField] private int currentIndex;

    public int CurrentIndex => currentIndex;
    public string CheckpointId => string.IsNullOrWhiteSpace(checkpointId)
        ? $"{gameObject.scene.name}/{gameObject.name}" : checkpointId;

    public void Configure(Transform rotor, PathGraph pathGraph, float[] angles)
    {
        rotatingRoot = rotor;
        graph = pathGraph;
        yawAngles = angles != null && angles.Length > 0 ? (float[])angles.Clone()
            : new[] { 0f, 90f, 180f, 270f };
        currentIndex = 0;
        ApplyRotation();
    }

    private void Awake() => ApplyRotation();

    public bool TryInteract(ActorKind actor)
    {
        if (actor != ActorKind.Player || yawAngles == null || yawAngles.Length < 2)
            return false;
        RotateNext();
        return true;
    }

    public void RotateNext()
    {
        if (yawAngles == null || yawAngles.Length == 0) return;
        currentIndex = (currentIndex + 1) % yawAngles.Length;
        ApplyRotation();
    }

    public string CaptureState() => JsonUtility.ToJson(new SavedState { index = currentIndex });

    public void RestoreState(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || yawAngles == null || yawAngles.Length == 0) return;
        currentIndex = Mathf.Clamp(JsonUtility.FromJson<SavedState>(json).index, 0, yawAngles.Length - 1);
        ApplyRotation();
    }

    private void ApplyRotation()
    {
        if (rotatingRoot == null || yawAngles == null || yawAngles.Length == 0) return;
        currentIndex = Mathf.Clamp(currentIndex, 0, yawAngles.Length - 1);
        rotatingRoot.localRotation = Quaternion.Euler(0f, yawAngles[currentIndex], 0f);
        Physics.SyncTransforms();
        graph?.Rebuild();
    }

    [Serializable]
    private struct SavedState { public int index; }
}
