using UnityEditor;

// Keep the old menu entry safe for anyone following an earlier Stage01 setup guide.
// The square-grid authoring command is now the single source of scene layout.
public static class Stage01PathGraphAuthoring
{
    [MenuItem("Tools/Stage01/Build Directed Path Graph")]
    public static void Build() => Stage01GridAuthoring.Build();
}
