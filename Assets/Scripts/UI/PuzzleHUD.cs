using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum Stage01TutorialStep
{
    AlignView,
    PlaceShadow,
    HoldRemotePlate,
    Complete,
    Finished
}

public enum Stage01TutorialOutline
{
    None,
    View,
    ShadowPlacement,
    RemotePlate
}

public sealed class PuzzleHUD : MonoBehaviour, IStopwatchFeedback
{
    [SerializeField] private Slider stopwatchDial;
    [SerializeField] private TMP_Text stopwatchDelayLabel;
    [SerializeField] private GameObject stopwatchPanel;
    [SerializeField] private TMP_Text tutorialLabel;
    [SerializeField] private GameObject[] viewOutlineObjects;
    [SerializeField] private GameObject[] shadowPlacementOutlineObjects;
    [SerializeField] private GameObject[] remotePlateOutlineObjects;
    [SerializeField] private AngleSnapCamera stage01Camera;
    [SerializeField] private LockableBridge stage01Bridge;
    [SerializeField] private PressurePlate stage01RemotePlate;
    [SerializeField] private StageGate stage01Gate;
    [SerializeField] private ShadowController stage01ShadowController;
    [SerializeField] private StageExit stage01Exit;
    [SerializeField] private CheckpointService stage01Checkpoint;
    [SerializeField] private CanvasGroup stage01CompletionPanel;
    [SerializeField, Min(0f)] private float tutorialFadeSeconds = 0.14f;
    [SerializeField, Min(0f)] private float checkpointToastSeconds = 1.2f;
    [SerializeField, Min(0f)] private float completionFadeSeconds = 0.5f;

    public Stage01TutorialStep CurrentTutorialStep { get; private set; }

    private bool isStage01TutorialActive;
    private Coroutine tutorialFadeRoutine;
    private Coroutine completionFadeRoutine;
    private bool tutorialMessageInitialized;
    private bool showingCheckpointToast;
    private float checkpointToastUntil;
    private CheckpointMarker observedCheckpoint;
    private int completionFrame = -1;
    private bool advancingStage;

    private void Awake()
    {
        ResolveStage01References();
    }

    private void Start()
    {
        isStage01TutorialActive = stage01Camera != null && stage01RemotePlate != null;
        if (isStage01TutorialActive)
        {
            SetStage01TutorialStep(Stage01TutorialStep.AlignView);
        }
    }

    private void Update()
    {
        if (!isStage01TutorialActive)
        {
            return;
        }

        var nextStep = stage01Exit != null && stage01Exit.IsComplete
            ? Stage01TutorialStep.Finished
            : AdvanceStage01Tutorial(
                CurrentTutorialStep,
                stage01Bridge != null && stage01Bridge.GetComponent<Collider>().enabled,
                stage01ShadowController != null && stage01ShadowController.HasShadow,
                stage01RemotePlate.IsPressed,
                stage01Gate != null && stage01Gate.IsOpen);

        if (nextStep != CurrentTutorialStep)
        {
            SetStage01TutorialStep(nextStep);
        }

        var activeCheckpoint = stage01Checkpoint != null ? stage01Checkpoint.ActiveMarker : null;
        if (activeCheckpoint != null && activeCheckpoint != observedCheckpoint)
        {
            observedCheckpoint = activeCheckpoint;
            if (CurrentTutorialStep != Stage01TutorialStep.Finished)
            {
                showingCheckpointToast = true;
                checkpointToastUntil = Time.time + checkpointToastSeconds;
                SetTutorialMessage("检查点已记录");
            }
        }

        if (showingCheckpointToast && Time.time >= checkpointToastUntil)
        {
            showingCheckpointToast = false;
            SetTutorialMessage(TutorialTextFor(CurrentTutorialStep));
        }

        if (CurrentTutorialStep == Stage01TutorialStep.Finished
            && Time.frameCount > completionFrame
            && ShouldConfirmCompletion(
                Input.GetMouseButtonDown(0),
                Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter),
                StageMenuReturn.IsPointerOverReturnButton(Input.mousePosition)))
        {
            TryAdvanceToNextStage();
        }
    }

    public static bool ShouldConfirmCompletion(bool clicked, bool confirmKey,
        bool overMenuButton) => confirmKey || (clicked && !overMenuButton);

    private void OnDisable()
    {
        if (tutorialFadeRoutine != null)
        {
            StopCoroutine(tutorialFadeRoutine);
            tutorialFadeRoutine = null;
        }

        if (completionFadeRoutine != null)
        {
            StopCoroutine(completionFadeRoutine);
            completionFadeRoutine = null;
        }
    }

    public static string TutorialTextFor(Stage01TutorialStep step)
    {
        switch (step)
        {
            case Stage01TutorialStep.AlignView:
                return "按 Z / X 切换视角，让道路对齐";
            case Stage01TutorialStep.PlaceShadow:
                return "左键点格子移动；按住 F 查看可放影子的点位，松开 F 投放";
            case Stage01TutorialStep.HoldRemotePlate:
                return "左键点前方格子；影子会同步跟随，送它踏上青色机关";
            case Stage01TutorialStep.Complete:
                return "大门已开启，左键点终点前进";
            case Stage01TutorialStep.Finished:
                return "第一关完成";
            default:
                return string.Empty;
        }
    }

    public static Stage01TutorialOutline OutlineFor(Stage01TutorialStep step)
    {
        switch (step)
        {
            case Stage01TutorialStep.AlignView:
                return Stage01TutorialOutline.View;
            case Stage01TutorialStep.PlaceShadow:
                return Stage01TutorialOutline.ShadowPlacement;
            case Stage01TutorialStep.HoldRemotePlate:
                return Stage01TutorialOutline.RemotePlate;
            default:
                return Stage01TutorialOutline.None;
        }
    }

    public static Stage01TutorialStep AdvanceStage01Tutorial(
        Stage01TutorialStep currentStep,
        bool cameraLocked,
        bool shadowExists,
        bool remotePlatePressed,
        bool gateOpen = false)
    {
        if (currentStep == Stage01TutorialStep.Finished)
        {
            return Stage01TutorialStep.Finished;
        }

        if (gateOpen || remotePlatePressed || currentStep == Stage01TutorialStep.Complete)
        {
            return Stage01TutorialStep.Complete;
        }

        if (!cameraLocked)
        {
            return Stage01TutorialStep.AlignView;
        }

        return shadowExists ? Stage01TutorialStep.HoldRemotePlate : Stage01TutorialStep.PlaceShadow;
    }

    public void SetStage01TutorialStep(Stage01TutorialStep step)
    {
        CurrentTutorialStep = step;

        if (step == Stage01TutorialStep.Finished)
        {
            completionFrame = Time.frameCount;
            showingCheckpointToast = false;
            if (tutorialFadeRoutine != null)
            {
                StopCoroutine(tutorialFadeRoutine);
                tutorialFadeRoutine = null;
            }

            if (tutorialLabel != null)
            {
                tutorialLabel.gameObject.SetActive(false);
            }

            var backdrop = transform.Find("TutorialBackdrop");
            if (backdrop != null)
            {
                backdrop.gameObject.SetActive(false);
            }

            ShowCompletionPanel();
        }
        else if (!showingCheckpointToast)
        {
            SetTutorialMessage(TutorialTextFor(step));
        }

        SetActive(viewOutlineObjects, OutlineFor(step) == Stage01TutorialOutline.View);
        SetActive(shadowPlacementOutlineObjects, OutlineFor(step) == Stage01TutorialOutline.ShadowPlacement);
        SetActive(remotePlateOutlineObjects, OutlineFor(step) == Stage01TutorialOutline.RemotePlate);
    }

    public void SetStopwatchUnlocked(bool unlocked)
    {
        if (stopwatchPanel != null)
        {
            stopwatchPanel.SetActive(unlocked);
        }
    }

    public bool TryAdvanceToNextStage()
    {
        const string nextScene = "Stage02_TurningPursuit";
        if (advancingStage || stage01Exit == null || !stage01Exit.IsComplete
            || CurrentTutorialStep != Stage01TutorialStep.Finished
            || !Application.CanStreamedLevelBeLoaded(nextScene))
        {
            return false;
        }

        advancingStage = true;
        SceneManager.LoadScene(nextScene);
        return true;
    }

    public void SetStopwatchDial(float delaySeconds, bool selecting, bool delayedMode)
    {
        var clampedDelay = Mathf.Clamp(delaySeconds, 0f, 4f);
        if (stopwatchDial != null)
        {
            stopwatchDial.minValue = 0f;
            stopwatchDial.maxValue = 4f;
            stopwatchDial.value = clampedDelay;
        }

        if (stopwatchDelayLabel != null)
        {
            stopwatchDelayLabel.text = selecting
                ? $"延迟 {clampedDelay:0.0}s"
                : delayedMode ? $"延迟模式 {clampedDelay:0.0}s" : "实时模式";
        }
    }

    private static void SetActive(GameObject[] objects, bool active)
    {
        if (objects == null)
        {
            return;
        }

        foreach (var outlineObject in objects)
        {
            if (outlineObject != null)
            {
                outlineObject.SetActive(active);
            }
        }
    }

    private void ResolveStage01References()
    {
        if (stage01Camera == null)
        {
            stage01Camera = FindFirstObjectByType<AngleSnapCamera>();
        }

        if (stage01RemotePlate == null)
        {
            stage01RemotePlate = FindFirstObjectByType<PressurePlate>();
        }

        if (stage01Bridge == null)
        {
            stage01Bridge = FindFirstObjectByType<LockableBridge>();
        }

        if (stage01Gate == null)
        {
            stage01Gate = FindFirstObjectByType<StageGate>();
        }

        if (stage01ShadowController == null)
        {
            stage01ShadowController = FindFirstObjectByType<ShadowController>();
        }

        if (stage01Exit == null)
        {
            stage01Exit = FindFirstObjectByType<StageExit>();
        }

        if (stage01Checkpoint == null)
        {
            stage01Checkpoint = FindFirstObjectByType<CheckpointService>();
        }

        if (stage01CompletionPanel == null)
        {
            var completionObject = transform.Find("Stage01_CompletionPanel");
            if (completionObject != null)
            {
                stage01CompletionPanel = completionObject.GetComponent<CanvasGroup>();
            }
        }
    }

    private void SetTutorialMessage(string message)
    {
        if (tutorialLabel == null)
        {
            return;
        }

        if (tutorialFadeRoutine != null)
        {
            StopCoroutine(tutorialFadeRoutine);
            tutorialFadeRoutine = null;
        }

        if (!Application.isPlaying || !tutorialMessageInitialized || tutorialFadeSeconds <= 0f)
        {
            tutorialLabel.text = message;
            SetTutorialAlpha(1f);
            tutorialMessageInitialized = true;
        }
        else
        {
            tutorialFadeRoutine = StartCoroutine(FadeTutorialTo(message));
        }
    }

    private void ShowCompletionPanel()
    {
        if (stage01CompletionPanel == null || stage01CompletionPanel.gameObject.activeSelf)
        {
            return;
        }

        var subtitle = stage01CompletionPanel.transform.Find("CompletionSubtitle");
        if (subtitle != null && subtitle.TryGetComponent<TMP_Text>(out var text))
        {
            text.text = "错位之桥 · 单击或按 Enter 进入第二关";
        }

        stage01CompletionPanel.alpha = 0f;
        stage01CompletionPanel.gameObject.SetActive(true);
        if (Application.isPlaying && completionFadeSeconds > 0f)
        {
            completionFadeRoutine = StartCoroutine(FadeCompletionPanel());
        }
        else
        {
            stage01CompletionPanel.alpha = 1f;
        }
    }

    private IEnumerator FadeCompletionPanel()
    {
        var elapsed = 0f;
        while (elapsed < completionFadeSeconds)
        {
            elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(elapsed / completionFadeSeconds);
            stage01CompletionPanel.alpha = Mathf.SmoothStep(0f, 1f, progress);
            yield return null;
        }

        stage01CompletionPanel.alpha = 1f;
        completionFadeRoutine = null;
    }

    private IEnumerator FadeTutorialTo(string message)
    {
        var fadeDuration = tutorialFadeSeconds * 0.5f;
        var startAlpha = tutorialLabel.color.a;
        var elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetTutorialAlpha(Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / fadeDuration)));
            yield return null;
        }

        tutorialLabel.text = message;
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetTutorialAlpha(Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        SetTutorialAlpha(1f);
        tutorialFadeRoutine = null;
    }

    private void SetTutorialAlpha(float alpha)
    {
        var color = tutorialLabel.color;
        color.a = alpha;
        tutorialLabel.color = color;
    }
}
