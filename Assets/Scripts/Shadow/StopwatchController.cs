using System;
using UnityEngine;

public sealed class StopwatchController : MonoBehaviour
{
    public const float MaximumDelaySeconds = 4f;
    public const float ClickThresholdSeconds = 0.1f;

    [SerializeField] private ShadowController shadowController;
    [SerializeField] private MonoBehaviour stopwatchFeedback;
    [SerializeField] private AudioSource clickAudioSource;

    private bool stopwatchUnlocked;
    private bool holdingSpace;
    private float heldSeconds;
    private float selectedDelaySeconds;
    private IStopwatchFeedback feedback;

    public float SelectedDelaySeconds => selectedDelaySeconds;
    public bool DelayModeActive => selectedDelaySeconds >= ClickThresholdSeconds;
    public event Action<float> DelayConfirmed;

    private void Awake()
    {
        if (shadowController == null)
        {
            shadowController = GetComponent<ShadowController>();
        }

        feedback = stopwatchFeedback as IStopwatchFeedback;
    }

    private void Update()
    {
        if (!stopwatchUnlocked)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            holdingSpace = true;
            heldSeconds = 0f;
            UpdateDialFeedback();
        }

        if (holdingSpace && Input.GetKey(KeyCode.Space))
        {
            heldSeconds = ClampDelay(heldSeconds + Time.deltaTime);
            UpdateDialFeedback();
        }

        if (holdingSpace && Input.GetKeyUp(KeyCode.Space))
        {
            ConfirmHeldDuration(heldSeconds);
        }
    }

    public void UnlockStopwatch()
    {
        stopwatchUnlocked = true;
        feedback?.SetStopwatchUnlocked(true);
        feedback?.SetStopwatchDial(selectedDelaySeconds, false, DelayModeActive);
    }

    public void ConfirmHeldDuration(float duration)
    {
        holdingSpace = false;
        selectedDelaySeconds = ResolveSelectedDelay(duration);
        shadowController?.SetDelay(selectedDelaySeconds);
        clickAudioSource?.Play();
        feedback?.SetStopwatchDial(selectedDelaySeconds, false, DelayModeActive);
        DelayConfirmed?.Invoke(selectedDelaySeconds);
    }

    public static float ClampDelay(float seconds)
    {
        return Mathf.Clamp(seconds, 0f, MaximumDelaySeconds);
    }

    public static float ResolveSelectedDelay(float heldDuration)
    {
        return heldDuration < ClickThresholdSeconds ? 0f : ClampDelay(heldDuration);
    }

    private void UpdateDialFeedback()
    {
        feedback?.SetStopwatchDial(heldSeconds, true, DelayModeActive);
    }
}
