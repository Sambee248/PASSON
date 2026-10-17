using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class StageMenuReturn : MonoBehaviour
{
    private bool returning;
    private RectTransform buttonRect;

    private void Awake()
    {
        if (EventSystem.current == null)
            new GameObject("StageMenuEventSystem", typeof(EventSystem),
                typeof(StandaloneInputModule));

        var canvasObject = new GameObject("ReturnToMenuCanvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var buttonObject = new GameObject("ReturnToMenuButton", typeof(RectTransform),
            typeof(Image), typeof(Button));
        var rect = buttonObject.GetComponent<RectTransform>();
        buttonRect = rect;
        rect.SetParent(canvasObject.transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-28f, -28f);
        rect.sizeDelta = new Vector2(225f, 60f);
        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.045f, 0.10f, 0.12f, 0.86f);
        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(ReturnToMenu);

        var labelObject = new GameObject("Label", typeof(RectTransform),
            typeof(TextMeshProUGUI));
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;
        labelRect.anchoredPosition = Vector2.zero;
        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = Resources.Load<TMP_FontAsset>("MenuCjk") ?? TMP_Settings.defaultFontAsset;
        label.text = "返回主界面  [Esc]";
        label.fontSize = 24f;
        label.color = new Color(0.94f, 0.95f, 0.87f);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) ReturnToMenu();
    }

    public static bool IsPointerOverReturnButton(Vector2 screenPosition)
    {
        var menu = FindFirstObjectByType<StageMenuReturn>();
        return menu != null && menu.buttonRect != null
               && RectTransformUtility.RectangleContainsScreenPoint(
                   menu.buttonRect, screenPosition);
    }

    public void ReturnToMenu()
    {
        if (returning) return;
        if (!Application.CanStreamedLevelBeLoaded("MainMenu"))
        {
            Debug.LogError("MainMenu scene is not available in build settings.");
            return;
        }

        returning = true;
        SceneManager.LoadScene("MainMenu");
    }
}
