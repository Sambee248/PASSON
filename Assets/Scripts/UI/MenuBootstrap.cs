using UnityEngine;
using UnityEngine.SceneManagement;

public static class MenuBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeAlreadyOpenScene() => EnsureCurrentScene();

    public static void EnsureCurrentScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.IsValid()) OnSceneLoaded(scene, LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            if (Object.FindFirstObjectByType<MainMenuController>() == null)
                new GameObject("MainMenuRuntime").AddComponent<MainMenuController>();
        }
        else if (scene.name == "Stage01_OffsetBridge"
                 || scene.name == "Stage02_TurningPursuit"
                 || scene.name == "Stage03_SharedShadowCourtyards")
        {
            if (Object.FindFirstObjectByType<StageMenuReturn>() == null)
                new GameObject("StageMenuReturnRuntime").AddComponent<StageMenuReturn>();
        }
    }
}
