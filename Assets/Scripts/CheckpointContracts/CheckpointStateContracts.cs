public interface ICheckpointState
{
    string CheckpointId { get; }

    string CaptureState();

    void RestoreState(string json);
}

public interface ICheckpointActorConfiguration
{
    string CaptureActorConfiguration();

    void RestoreActorConfiguration(string json);
}
