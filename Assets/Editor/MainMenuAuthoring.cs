using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MainMenuAuthoring
{
    private const string MenuPath = "Assets/Scenes/MainMenu.unity";
    private const string OpenedKey = "ShadowRelay.MainMenuOpenedThisSession";

    static MainMenuAuthoring()
    {
        EditorApplication.delayCall += OpenOnFirstImport;
    }

    [MenuItem("Tools/Game/Open Main Menu")]
    public static void OpenMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuPath) == null)
        {
            Debug.LogError("Main menu scene is missing.");
            return;
        }
        EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
    }

    private static void OpenOnFirstImport()
    {
        if (SessionState.GetBool(OpenedKey, false)
            || EditorApplication.isPlayingOrWillChangePlaymode
            || EditorApplication.isCompiling) return;

        var current = SceneManager.GetActiveScene();
        if (!current.IsValid() || current.isDirty) return;
        if (current.path != "Assets/Scenes/Stage01_OffsetBridge.unity"
            && current.path != "Assets/Scenes/Stage02_TurningPursuit.unity") return;

        SessionState.SetBool(OpenedKey, true);
        OpenMenu();
    }
}
