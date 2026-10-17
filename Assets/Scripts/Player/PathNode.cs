using UnityEngine;

[DisallowMultipleComponent]
public sealed class PathNode : MonoBehaviour
{
    public Vector3 Position => transform.position;
}
