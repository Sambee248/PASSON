using UnityEngine;

[DisallowMultipleComponent]
public sealed class PathClickFeedback : MonoBehaviour
{
    [SerializeField, Min(0f)] private float rejectionSeconds = 0.45f;

    private Transform destinationMarker;
    private Transform rejectedMarker;
    private Mesh markerMesh;
    private Material destinationMaterial;
    private Material rejectedMaterial;
    private Vector3 destinationPosition;
    private Vector3 rejectedPosition;
    private float rejectionUntil;

    public bool DestinationVisible => destinationMarker != null && destinationMarker.gameObject.activeSelf;
    public bool RejectedVisible => rejectedMarker != null && rejectedMarker.gameObject.activeSelf;

    public void ShowDestination(Vector3 worldPosition)
    {
        EnsureMarkers();
        destinationPosition = worldPosition + Vector3.up * 0.055f;
        destinationMarker.gameObject.SetActive(true);
        rejectedMarker.gameObject.SetActive(false);
    }

    public void ShowRejected(Vector3 worldPosition)
    {
        EnsureMarkers();
        rejectedPosition = worldPosition + Vector3.up * 0.065f;
        rejectionUntil = Time.time + rejectionSeconds;
        rejectedMarker.gameObject.SetActive(true);
    }

    public void HideDestination()
    {
        if (destinationMarker != null) destinationMarker.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (DestinationVisible)
        {
            destinationMarker.position = destinationPosition;
            destinationMarker.localScale = Vector3.one * (0.66f + 0.05f * Mathf.Sin(Time.time * 8f));
        }
        if (RejectedVisible)
        {
            if (Time.time >= rejectionUntil) rejectedMarker.gameObject.SetActive(false);
            else
            {
                rejectedMarker.position = rejectedPosition;
                rejectedMarker.localScale = Vector3.one * (0.72f + 0.08f * Mathf.Sin(Time.time * 12f));
            }
        }
    }

    private void OnDisable()
    {
        HideDestination();
        if (rejectedMarker != null) rejectedMarker.gameObject.SetActive(false);
    }

    private void EnsureMarkers()
    {
        if (destinationMarker != null) return;
        markerMesh = new Mesh { name = "PathClickDiamond" };
        markerMesh.vertices = new[]
        {
            new Vector3(-0.5f, 0f, 0f), new Vector3(0f, 0f, 0.5f),
            new Vector3(0.5f, 0f, 0f), new Vector3(0f, 0f, -0.5f)
        };
        markerMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        markerMesh.RecalculateNormals();
        var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        destinationMaterial = new Material(shader) { color = new Color(0.14f, 0.95f, 1f) };
        rejectedMaterial = new Material(shader) { color = new Color(1f, 0.24f, 0.3f) };
        destinationMarker = CreateMarker("PathDestination", destinationMaterial);
        rejectedMarker = CreateMarker("PathBlocked", rejectedMaterial);
    }

    private Transform CreateMarker(string markerName, Material material)
    {
        var root = new GameObject(markerName);
        root.transform.SetParent(transform, false);
        root.AddComponent<MeshFilter>().sharedMesh = markerMesh;
        root.AddComponent<MeshRenderer>().sharedMaterial = material;
        root.SetActive(false);
        return root.transform;
    }

    private void OnDestroy()
    {
        if (markerMesh != null) Destroy(markerMesh);
        if (destinationMaterial != null) Destroy(destinationMaterial);
        if (rejectedMaterial != null) Destroy(rejectedMaterial);
    }
}
