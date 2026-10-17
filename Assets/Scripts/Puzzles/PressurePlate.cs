using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Flags]
public enum AcceptedActors
{
    None = 0,
    Player = 1 << 0,
    Shadow = 1 << 1,
    Either = Player | Shadow
}

public sealed class PressurePlate : MonoBehaviour, ICheckpointState
{
    [SerializeField] private string checkpointId;
    [SerializeField] private AcceptedActors acceptedActors = AcceptedActors.Either;
    [SerializeField] private UnityEvent activated = new UnityEvent();
    [SerializeField] private UnityEvent deactivated = new UnityEvent();

    private readonly HashSet<Collider> pressingColliders = new HashSet<Collider>();

    public bool IsPressed { get; private set; }
    public UnityEvent Activated => activated;

    public string CheckpointId => string.IsNullOrWhiteSpace(checkpointId)
        ? $"{gameObject.scene.name}/{gameObject.name}"
        : checkpointId;

    private void Reset()
    {
        var trigger = GetComponent<Collider>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PruneInvalidColliders();
        if (Accepts(other))
        {
            pressingColliders.Add(other);
            RefreshPressedState();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (pressingColliders.Remove(other))
        {
            RefreshPressedState();
        }
    }

    private void FixedUpdate()
    {
        PruneInvalidColliders();
    }

    public string CaptureState()
    {
        return JsonUtility.ToJson(new PlateState { pressed = IsPressed });
    }

    public void RestoreState(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        SetPressed(JsonUtility.FromJson<PlateState>(json).pressed);
    }

    public void SetPressedForTest(bool pressed)
    {
        SetPressed(pressed);
    }

    private bool Accepts(Collider candidate)
    {
        if (!TryGetActor(candidate, out var actor))
        {
            return false;
        }

        var requiredFlag = actor == ActorKind.Player ? AcceptedActors.Player : AcceptedActors.Shadow;
        return (acceptedActors & requiredFlag) != 0;
    }

    private static bool TryGetActor(Collider candidate, out ActorKind actor)
    {
        var playerLayer = LayerMask.NameToLayer("Player");
        if (candidate.gameObject.layer == playerLayer || candidate.CompareTag("Player"))
        {
            actor = ActorKind.Player;
            return true;
        }

        var shadowLayer = LayerMask.NameToLayer("Shadow");
        if (shadowLayer >= 0 && candidate.gameObject.layer == shadowLayer)
        {
            actor = ActorKind.Shadow;
            return true;
        }

        actor = default;
        return false;
    }

    private void RefreshPressedState()
    {
        SetPressed(pressingColliders.Count > 0);
    }

    private void PruneInvalidColliders()
    {
        if (pressingColliders.RemoveWhere(collider => collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy) > 0)
        {
            RefreshPressedState();
        }
    }

    private void SetPressed(bool pressed)
    {
        if (IsPressed == pressed)
        {
            return;
        }

        IsPressed = pressed;
        if (IsPressed)
        {
            activated?.Invoke();
        }
        else
        {
            deactivated?.Invoke();
        }
    }

    [Serializable]
    private struct PlateState
    {
        public bool pressed;
    }
}
