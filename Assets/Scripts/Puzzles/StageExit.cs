using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class StageExit : MonoBehaviour
{
    public bool IsComplete { get; private set; }

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            IsComplete = true;
        }
    }
}
