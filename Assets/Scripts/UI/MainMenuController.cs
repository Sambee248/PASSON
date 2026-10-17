using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
    private const string StageOne = "Stage01_OffsetBridge";
    private const string StageTwo = "Stage02_TurningPursuit";
    private const string StageThree = "Stage03_SharedShadowCourtyards";

    private TMP_FontAsset font;
    private GameObject homePanel;
    private GameObject levelSelectPanel;
    private TMP_Text statusLabel;
    private bool loading;

    private void Awake()
    {
        font = Resources.Load<TMP_FontAsset>("MenuCjk") ?? TMP_Settings.defaultFontAsset;
        if (EventSystem.current == null)
            new GameObject("MainMenuEventSystem", typeof(EventSystem),
                typeof(StandaloneInputModule));
        BuildInterface();
        ShowHome();
    }

    private void Update()
    {
        if (!loading && levelSelectPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            ShowHome();
    }

    public void ShowHome()
    {
        homePanel.SetActive(true);
        levelSelectPanel.SetActive(false);
        statusLabel.text = string.Empty;
    }

    public void ShowLevelSelect()
    {
        homePanel.SetActive(false);
        levelSelectPanel.SetActive(true);
        statusLabel.text = string.Empty;
    }

    public void StartGame() => LoadStage(StageOne);
    public void OpenStageOne() => LoadStage(StageOne);
    public void OpenStageTwo() => LoadStage(StageTwo);
    public void OpenStageThree() => LoadStage(StageThree);

    private void LoadStage(string sceneName)
    {
        if (loading) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            statusLabel.text = "关卡尚未加入构建列表";
            Debug.LogError($"Cannot load missing stage: {sceneName}");
            return;
        }

        loading = true;
        SceneManager.LoadScene(sceneName);
    }

    private void BuildInterface()
    {
        var canvasObject = new GameObject("MainMenuCanvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var root = canvasObject.GetComponent<RectTransform>();
        AddImage(root, "Background", Vector2.zero, Vector2.one, Vector2.zero,
            Vector2.zero, new Color(0.047f, 0.077f, 0.095f));
        AddImage(root, "LeftGlow", new Vector2(0f, 0f), new Vector2(0.55f, 1f),
            Vector2.zero, Vector2.zero, new Color(0.063f, 0.105f, 0.122f));
        BuildArchitecture(root);

        homePanel = CreatePanel(root, "HomePanel");
        var home = CreateRect(homePanel.transform, "HomeContent", new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(120f, 0f), new Vector2(850f, 800f));
        AddText(home, "Eyebrow", "S H A D O W   R E L A Y", 24,
            new Color(0.67f, 0.81f, 0.79f), new Vector2(0f, 300f),
            new Vector2(760f, 42f));
        AddText(home, "Title", "影之回廊", 100, new Color(0.97f, 0.93f, 0.82f),
            new Vector2(0f, 182f), new Vector2(780f, 145f));
        AddText(home, "Subtitle", "让光与影在不可能的道路上相遇", 30,
            new Color(0.72f, 0.80f, 0.78f), new Vector2(0f, 99f),
            new Vector2(760f, 70f));
        AddButton(home, "StartGameButton", "开始游戏", new Vector2(0f, -35f),
            new Color(0.79f, 0.66f, 0.43f), new Color(0.07f, 0.10f, 0.11f),
            StartGame);
        AddButton(home, "ChooseLevelButton", "选  择  关  卡",
            new Vector2(0f, -157f), new Color(0.16f, 0.34f, 0.37f),
            new Color(0.93f, 0.96f, 0.91f), ShowLevelSelect);
        AddText(home, "HomeHint", "点击格子移动  ·  Z / X 旋转视角  ·  F 投放影子", 23,
            new Color(0.55f, 0.68f, 0.68f), new Vector2(0f, -284f),
            new Vector2(800f, 54f));

        levelSelectPanel = CreatePanel(root, "LevelSelectPanel");
        var levels = CreateRect(levelSelectPanel.transform, "LevelContent",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(120f, 0f), new Vector2(880f, 800f));
        AddText(levels, "LevelEyebrow", "C H O O S E   A   P A T H", 24,
            new Color(0.67f, 0.81f, 0.79f), new Vector2(0f, 300f),
            new Vector2(780f, 42f));
        AddText(levels, "LevelTitle", "选择关卡", 76,
            new Color(0.97f, 0.93f, 0.82f), new Vector2(0f, 210f),
            new Vector2(790f, 120f));
        AddButton(levels, "Stage01Button", "第一关  ·  错位之桥",
            new Vector2(0f, 80f), new Color(0.16f, 0.34f, 0.37f),
            Color.white, OpenStageOne, new Vector2(650f, 100f));
        AddButton(levels, "Stage02Button", "第二关  ·  旋转追迹",
            new Vector2(0f, -40f), new Color(0.23f, 0.31f, 0.35f),
            Color.white, OpenStageTwo, new Vector2(650f, 100f));
        AddButton(levels, "Stage03Button", "第三关  ·  双庭共影",
            new Vector2(0f, -160f), new Color(0.16f, 0.38f, 0.40f),
            Color.white, OpenStageThree, new Vector2(650f, 100f));
        AddButton(levels, "BackButton", "←  返回主界面",
            new Vector2(0f, -290f), new Color(0.10f, 0.20f, 0.23f),
            new Color(0.80f, 0.91f, 0.86f), ShowHome,
            new Vector2(360f, 78f));

        statusLabel = AddText(root, "MenuStatus", string.Empty, 26,
            new Color(1f, 0.54f, 0.39f), Vector2.zero,
            new Vector2(920f, 55f), TextAlignmentOptions.Center);
        AnchorToBottom(statusLabel.rectTransform, 72f);
        var footer = AddText(root, "Footer", "PASSON  /  01—03", 19,
            new Color(0.45f, 0.58f, 0.59f), Vector2.zero,
            new Vector2(550f, 40f), TextAlignmentOptions.Center);
        AnchorToBottom(footer.rectTransform, 24f);
    }

    private static void AnchorToBottom(RectTransform rect, float bottom)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, bottom);
    }

    private static GameObject CreatePanel(RectTransform parent, string name)
    {
        return CreateRect(parent, name, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero).gameObject;
    }

    private static RectTransform CreateRect(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static RectTransform AddImage(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
    {
        var rect = CreateRect(parent, name, anchorMin, anchorMax,
            new Vector2(0.5f, 0.5f), position, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private TMP_Text AddText(Transform parent, string name, string value, float size,
        Color color, Vector2 position, Vector2 dimensions,
        TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        var rect = CreateRect(parent, name, new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), position, dimensions);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.enableWordWrapping = false;
        label.raycastTarget = false;
        label.text = value;
        return label;
    }

    private void AddButton(Transform parent, string name, string title, Vector2 position,
        Color background, Color foreground, UnityEngine.Events.UnityAction action,
        Vector2? dimensions = null)
    {
        var size = dimensions ?? new Vector2(430f, 94f);
        var rect = CreateRect(parent, name, new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), position, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = background;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        button.colors = colors;
        button.onClick.AddListener(action);
        var text = CreateRect(rect, "Label", Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
            .gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = 31f;
        text.fontStyle = FontStyles.Bold;
        text.color = foreground;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        text.text = title;
    }

    private static void BuildArchitecture(RectTransform root)
    {
        var stone = new Color(0.14f, 0.27f, 0.30f, 1f);
        var face = new Color(0.22f, 0.42f, 0.43f, 1f);
        var lit = new Color(0.70f, 0.62f, 0.47f, 1f);
        var shadow = new Color(0.035f, 0.067f, 0.08f, 1f);
        var tower = CreateRect(root, "ArchitecturalMotif", new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-420f, -10f), new Vector2(710f, 780f));
        AddImage(tower, "TowerShadow", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(65f, -175f),
            new Vector2(530f, 210f), shadow).localRotation = Quaternion.Euler(0f, 0f, -24f);
        AddImage(tower, "LeftPillar", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(-170f, -50f),
            new Vector2(138f, 520f), stone);
        AddImage(tower, "RightPillar", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(165f, 20f),
            new Vector2(138f, 520f), stone);
        AddImage(tower, "FloatingBridge", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(5f, 67f),
            new Vector2(510f, 62f), face).localRotation = Quaternion.Euler(0f, 0f, 18f);
        AddImage(tower, "UpperLanding", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(165f, 303f),
            new Vector2(230f, 39f), lit);
        AddImage(tower, "LowerLanding", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(-170f, -320f),
            new Vector2(230f, 39f), lit);
        AddImage(tower, "ShadowSquare", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(210f, -255f),
            new Vector2(90f, 90f), lit).localRotation = Quaternion.Euler(0f, 0f, 45f);
    }
}
