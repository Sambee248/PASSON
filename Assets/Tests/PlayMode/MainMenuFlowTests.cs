using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;

public sealed class MainMenuFlowTests
{
    [UnityTest]
    public IEnumerator Start_button_opens_first_stage()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu");
        Assert.That(GameObject.Find("MainMenuCanvas")?.GetComponent<GraphicRaycaster>(),
            Is.Not.Null);
        GameObject.Find("StartGameButton").GetComponent<Button>().onClick.Invoke();
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "Stage01_OffsetBridge");
        Assert.That(GameObject.Find("Stage01_T7_Player"), Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator Already_open_menu_scene_can_initialize_without_a_scene_load_event()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu");
        Object.Destroy(Object.FindFirstObjectByType<MainMenuController>().gameObject);
        yield return null;

        var initializer = typeof(MenuBootstrap).GetMethod("EnsureCurrentScene",
            BindingFlags.Public | BindingFlags.Static);
        Assert.That(initializer, Is.Not.Null);
        initializer.Invoke(null, null);
        Assert.That(GameObject.Find("StartGameButton")?.GetComponent<Button>(), Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator First_stage_has_a_working_return_button()
    {
        yield return SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        var button = GameObject.Find("ReturnToMenuButton")?.GetComponent<Button>();
        Assert.That(button, Is.Not.Null);
        button.onClick.Invoke();
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "MainMenu");
        Assert.That(GameObject.Find("StartGameButton"), Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator Level_select_back_restores_start_button()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu");
        GameObject.Find("ChooseLevelButton").GetComponent<Button>().onClick.Invoke();
        GameObject.Find("BackButton").GetComponent<Button>().onClick.Invoke();
        Assert.That(GameObject.Find("StartGameButton"), Is.Not.Null);
        Assert.That(GameObject.Find("LevelSelectPanel"), Is.Null);
    }

    [UnityTest]
    public IEnumerator Bottom_messages_stay_inside_the_visible_menu_canvas()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu");
        Canvas.ForceUpdateCanvases();
        var canvas = GameObject.Find("MainMenuCanvas").GetComponent<RectTransform>();
        var footer = GameObject.Find("Footer").GetComponent<RectTransform>();
        var status = GameObject.Find("MenuStatus").GetComponent<RectTransform>();
        var canvasCorners = new Vector3[4];
        var footerCorners = new Vector3[4];
        var statusCorners = new Vector3[4];
        canvas.GetWorldCorners(canvasCorners);
        footer.GetWorldCorners(footerCorners);
        status.GetWorldCorners(statusCorners);
        Assert.That(footerCorners[0].y, Is.GreaterThanOrEqualTo(canvasCorners[0].y),
            "The footer must not disappear below an ultrawide Game view.");
        Assert.That(statusCorners[0].y, Is.GreaterThanOrEqualTo(canvasCorners[0].y),
            "A missing-stage error must remain visible.");
    }

    [UnityTest]
    public IEnumerator Level_select_opens_second_stage_and_can_return_to_menu()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu");
        GameObject.Find("ChooseLevelButton").GetComponent<Button>().onClick.Invoke();
        Assert.That(GameObject.Find("LevelSelectPanel").activeSelf, Is.True);
        Assert.That(GameObject.Find("Stage01Button").GetComponentInChildren<TMP_Text>().text,
            Does.Contain("第一关"));
        Assert.That(GameObject.Find("Stage02Button").GetComponentInChildren<TMP_Text>().text,
            Does.Contain("第二关"));
        GameObject.Find("Stage02Button").GetComponent<Button>().onClick.Invoke();
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "Stage02_TurningPursuit");
        GameObject.Find("ReturnToMenuButton").GetComponent<Button>().onClick.Invoke();
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "MainMenu");
        Assert.That(GameObject.Find("StartGameButton"), Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator Level_select_opens_third_stage_and_can_return_to_menu()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu");
        GameObject.Find("ChooseLevelButton").GetComponent<Button>().onClick.Invoke();
        var stageButton = GameObject.Find("Stage03Button")?.GetComponent<Button>();
        Assert.That(stageButton, Is.Not.Null);
        Assert.That(stageButton.GetComponentInChildren<TMP_Text>().text, Does.Contain("第三关"));
        stageButton.onClick.Invoke();
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name ==
            "Stage03_SharedShadowCourtyards");
        var returnButton = GameObject.Find("ReturnToMenuButton")?.GetComponent<Button>();
        Assert.That(returnButton, Is.Not.Null);
        returnButton.onClick.Invoke();
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "MainMenu");
        Assert.That(GameObject.Find("StartGameButton"), Is.Not.Null);
    }
}
