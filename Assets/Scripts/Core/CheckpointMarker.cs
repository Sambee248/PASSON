using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class CheckpointMarker : MonoBehaviour
{
    [SerializeField] private Transform respawnTransform;

    public Transform RespawnTransform => respawnTransform != null ? respawnTransform : transform;

    private void Reset()
    {
        var trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FindObjectOfType<CheckpointService>()?.Activate(this);
        }
    }
}
