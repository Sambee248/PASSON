public interface IStopwatchFeedback
{
    void SetStopwatchUnlocked(bool unlocked);
    void SetStopwatchDial(float delaySeconds, bool selecting, bool delayedMode);
}
