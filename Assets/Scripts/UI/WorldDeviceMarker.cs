using UnityEngine;

public sealed class WorldDeviceMarker : MonoBehaviour
{
    [SerializeField] private Transform anchor;
    [SerializeField] private Vector3 worldOffset;

    public void Configure(Transform target, Vector3 offset)
    {
        anchor = target;
        worldOffset = offset;
        RefreshPose();
    }

    private void LateUpdate() => RefreshPose();

    public void RefreshPose()
    {
        if (anchor != null)
            transform.position = anchor.position + worldOffset;

        var camera = Camera.main;
        if (camera != null)
            transform.rotation = camera.transform.rotation;
    }
}
