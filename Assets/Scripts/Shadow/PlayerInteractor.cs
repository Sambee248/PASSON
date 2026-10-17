using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private PlayerCommandSource commandSource;
    [SerializeField] private PlayerPathController pathController;
    [SerializeField, Min(0.1f)] private float interactionRadius = 1.8f;

    private void Awake()
    {
        if (commandSource == null) commandSource = GetComponent<PlayerCommandSource>();
        if (pathController == null) pathController = GetComponent<PlayerPathController>();
    }

    private void OnEnable()
    {
        if (commandSource != null) commandSource.CommandIssued += HandleCommand;
    }

    private void OnDisable()
    {
        if (commandSource != null) commandSource.CommandIssued -= HandleCommand;
    }

    public void Configure(PlayerCommandSource source, PlayerPathController path, float radius)
    {
        if (isActiveAndEnabled && commandSource != null)
            commandSource.CommandIssued -= HandleCommand;
        commandSource = source;
        pathController = path;
        interactionRadius = Mathf.Max(0.1f, radius);
        if (isActiveAndEnabled && commandSource != null)
            commandSource.CommandIssued += HandleCommand;
    }

    private void HandleCommand(PlayerCommand command)
    {
        if (command.Type == PlayerCommandType.Interact) TryInteractNow();
    }

    public bool TryInteractNow()
    {
        if (pathController != null && pathController.IsMoving) return false;
        var center = transform.position + Vector3.up * 0.8f;
        var hits = Physics.OverlapSphere(center, interactionRadius,
            ~0, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (left, right) =>
            (left.ClosestPoint(center) - center).sqrMagnitude.CompareTo(
                (right.ClosestPoint(center) - center).sqrMagnitude));
        foreach (var hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            var interactable = hit.GetComponentInParent<IActorInteractable>();
            if (interactable != null && interactable.TryInteract(ActorKind.Player)) return true;
        }
        return false;
    }
}
