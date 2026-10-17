using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public interface ICheckpointPathActor
{
    bool RestoreToNearestNode(Vector3 position, Quaternion rotation);
}

public sealed class CheckpointService : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private MonoBehaviour shadowConfigurationSource;

    private readonly Dictionary<string, ICheckpointState> registeredStates = new Dictionary<string, ICheckpointState>();
    private readonly Dictionary<string, string> latestMechanismStates = new Dictionary<string, string>();
    private readonly HashSet<string> duplicateCheckpointIds = new HashSet<string>();
    private TransformState latestPlayerState;
    private string latestShadowConfiguration;
    private CheckpointMarker activeMarker;

    public CheckpointMarker ActiveMarker => activeMarker;
    public int RegisteredCheckpointCount => registeredStates.Count;
    public IReadOnlyCollection<string> DuplicateCheckpointIds => duplicateCheckpointIds;

    private void Awake()
    {
        ResolvePlayerTransform();
        RegisterSceneComponents();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public void Activate(CheckpointMarker marker)
    {
        if (marker == null)
        {
            throw new ArgumentNullException(nameof(marker));
        }

        ResolvePlayerTransform();
        RegisterSceneComponents();
        activeMarker = marker;
        latestMechanismStates.Clear();

        foreach (var registeredState in registeredStates)
        {
            latestMechanismStates[registeredState.Key] = registeredState.Value.CaptureState();
        }

        latestPlayerState = TransformState.From(marker.RespawnTransform != null ? marker.RespawnTransform : playerTransform);
        var shadow = shadowConfigurationSource as ICheckpointActorConfiguration;
        latestShadowConfiguration = shadow != null ? shadow.CaptureActorConfiguration() : null;
    }

    public void NotifyPlayerFailure()
    {
        if (activeMarker != null)
        {
            RestoreLatestCheckpoint();
            return;
        }

        // Before the first checkpoint, a scene reload restores every puzzle
        // mechanism as well as the player and shadow to their authored start.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void RestoreLatestCheckpoint()
    {
        if (activeMarker == null)
        {
            return;
        }

        ResolvePlayerTransform();
        var pathController = playerTransform != null
            ? playerTransform.GetComponent<ICheckpointPathActor>() : null;
        if (pathController != null)
        {
            if (!pathController.RestoreToNearestNode(latestPlayerState.position, latestPlayerState.rotation))
            {
                Debug.LogError("Checkpoint restore could not find a player path node.", playerTransform);
            }
        }
        else
        {
            latestPlayerState.ApplyTo(playerTransform);
        }

        var shadow = shadowConfigurationSource as ICheckpointActorConfiguration;
        if (shadow != null && !string.IsNullOrWhiteSpace(latestShadowConfiguration))
        {
            shadow.RestoreActorConfiguration(latestShadowConfiguration);
        }

        RegisterSceneComponents();
        foreach (var savedState in latestMechanismStates)
        {
            if (registeredStates.TryGetValue(savedState.Key, out var state))
            {
                state.RestoreState(savedState.Value);
            }
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        registeredStates.Clear();
        RegisterSceneComponents();
    }

    private void ResolvePlayerTransform()
    {
        if (playerTransform != null)
        {
            return;
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void RegisterSceneComponents()
    {
        registeredStates.Clear();
        duplicateCheckpointIds.Clear();

        foreach (var behaviour in FindObjectsOfType<MonoBehaviour>(true))
        {
            if (behaviour is ICheckpointState state && !string.IsNullOrWhiteSpace(state.CheckpointId))
            {
                if (!registeredStates.TryAdd(state.CheckpointId, state))
                {
                    duplicateCheckpointIds.Add(state.CheckpointId);
                    Debug.LogError($"Duplicate checkpoint ID '{state.CheckpointId}' on '{behaviour.name}'. The first registration remains active.", behaviour);
                }
            }
        }
    }

    [Serializable]
    private struct TransformState
    {
        public Vector3 position;
        public Quaternion rotation;

        public static TransformState From(Transform source)
        {
            return source == null
                ? default
                : new TransformState { position = source.position, rotation = source.rotation };
        }

        public void ApplyTo(Transform destination)
        {
            if (destination != null)
            {
                destination.SetPositionAndRotation(position, rotation);
            }
        }
    }
}
