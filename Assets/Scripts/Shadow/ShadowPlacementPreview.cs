using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShadowPlacementPreview : MonoBehaviour
{
    private readonly List<MeshRenderer> markers = new List<MeshRenderer>();
    private Mesh markerMesh;
    private Material normalMaterial;
    private Material hoverMaterial;

    public int VisibleCount { get; private set; }
    public GridCell HoveredCell { get; private set; }

    public void Show(IReadOnlyList<GridCell> eligible, GridCell hovered)
    {
        var count = eligible != null ? eligible.Count : 0;
        EnsureMarkers(count);
        HoveredCell = null;
        VisibleCount = count;
        for (var index = 0; index < markers.Count; index++)
        {
            var marker = markers[index];
            var visible = index < count && eligible[index] != null;
            marker.gameObject.SetActive(visible);
            if (!visible) continue;

            var cell = eligible[index];
            marker.transform.position = cell.SurfacePoint + Vector3.up * 0.075f;
            var isHover = cell == hovered;
            marker.sharedMaterial = isHover ? hoverMaterial : normalMaterial;
            marker.transform.localScale = Vector3.one * (isHover ? 0.58f : 0.42f);
            if (isHover) HoveredCell = cell;
        }
    }

    public void Hide()
    {
        foreach (var marker in markers) marker.gameObject.SetActive(false);
        HoveredCell = null;
        VisibleCount = 0;
    }

    private void EnsureMarkers(int count)
    {
        if (markers.Count >= count) return;
        if (markerMesh == null) markerMesh = CreateDiamondMesh();
        if (normalMaterial == null)
        {
            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            normalMaterial = new Material(shader) { color = new Color(0.95f, 0.99f, 1f, 1f) };
            hoverMaterial = new Material(shader) { color = new Color(1f, 0.76f, 0.35f, 1f) };
        }
        while (markers.Count < count)
        {
            var dot = new GameObject("ValidShadowCell_" + markers.Count);
            dot.transform.SetParent(transform, false);
            dot.AddComponent<MeshFilter>().sharedMesh = markerMesh;
            var renderer = dot.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = normalMaterial;
            markers.Add(renderer);
        }
    }

    private static Mesh CreateDiamondMesh()
    {
        var mesh = new Mesh { name = "ShadowPlacementDiamond" };
        mesh.vertices = new[]
        {
            new Vector3(0f, 0.32f, 0f), new Vector3(0f, -0.16f, 0f),
            new Vector3(0.5f, 0f, 0f), new Vector3(0f, 0f, 0.5f),
            new Vector3(-0.5f, 0f, 0f), new Vector3(0f, 0f, -0.5f)
        };
        mesh.triangles = new[]
        {
            0, 2, 3, 0, 3, 4, 0, 4, 5, 0, 5, 2,
            1, 3, 2, 1, 4, 3, 1, 5, 4, 1, 2, 5
        };
        mesh.RecalculateNormals();
        return mesh;
    }

    private void OnDestroy()
    {
        if (Application.isPlaying)
        {
            if (markerMesh != null) Destroy(markerMesh);
            if (normalMaterial != null) Destroy(normalMaterial);
            if (hoverMaterial != null) Destroy(hoverMaterial);
        }
        else
        {
            if (markerMesh != null) DestroyImmediate(markerMesh);
            if (normalMaterial != null) DestroyImmediate(normalMaterial);
            if (hoverMaterial != null) DestroyImmediate(hoverMaterial);
        }
    }
}
