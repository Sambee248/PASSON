using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ShadowController : MonoBehaviour, ICheckpointActorConfiguration
{
    [SerializeField] private GameObject shadowPrefab;
    [SerializeField] private PlayerCommandSource playerCommandSource;
    [SerializeField] private PlayerPathController playerPathController;
    [SerializeField] private float emergenceDepth = 1.2f;
    [SerializeField] private float emergenceDuration = 0.35f;
    [SerializeField] private float interactionRadius = 1.25f;

    private readonly Queue<PlayerCommand> pendingCommands = new Queue<PlayerCommand>();
    private readonly Queue<ScheduledCommand> delayedCommands = new Queue<ScheduledCommand>();
    private GameObject shadowInstance;
    private ShadowFollower shadowFollower;
    private float delaySeconds;
    private float lastScheduledExecutionTime = float.NegativeInfinity;
    private Func<float> timeProvider;
    private Coroutine emergenceRoutine;

    public bool HasShadow => shadowInstance != null;
    public ShadowFollower ActiveShadow => shadowFollower;

    private void Awake()
    {
        if (playerCommandSource == null)
        {
            playerCommandSource = GetComponent<PlayerCommandSource>();
        }
        if (playerPathController == null)
        {
            playerPathController = GetComponent<PlayerPathController>();
        }
    }

    private void OnEnable()
    {
        if (playerCommandSource != null)
        {
            playerCommandSource.CommandIssued += QueueCommand;
        }
        if (playerPathController != null)
        {
            playerPathController.TravelDisplacement += OnPlayerTravel;
        }
    }

    private void OnDisable()
    {
        if (playerCommandSource != null)
        {
            playerCommandSource.CommandIssued -= QueueCommand;
        }
        if (playerPathController != null)
        {
            playerPathController.TravelDisplacement -= OnPlayerTravel;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Recall();
        }

        while (delayedCommands.Count > 0 && delayedCommands.Peek().ExecuteAt <= CurrentTime)
        {
            Execute(delayedCommands.Dequeue().Command);
        }
    }

    private void FixedUpdate()
    {
        if (!HasShadow)
        {
            return;
        }

        while (pendingCommands.Count > 0)
        {
            Execute(pendingCommands.Dequeue());
        }
    }

    public bool TrySpawn(Vector3 walkablePoint)
    {
        if (shadowPrefab == null)
        {
            return false;
        }

        var spawned = Instantiate(shadowPrefab, GetEmergenceStart(walkablePoint, emergenceDepth), Quaternion.identity);
        var spawnedFollower = spawned.GetComponent<ShadowFollower>();
        if (spawnedFollower == null)
        {
            DestroyShadowObject(spawned);
            return false;
        }

        SetLayerRecursively(spawned, LayerMask.NameToLayer("Shadow"));

        if (shadowInstance != null)
        {
            if (shadowFollower != null)
            {
                shadowFollower.Fell -= Recall;
            }

            DestroyShadowObject(shadowInstance);
        }

        shadowInstance = spawned;
        shadowFollower = spawnedFollower;
        shadowFollower.Fell += Recall;
        pendingCommands.Clear();
        delayedCommands.Clear();
        lastScheduledExecutionTime = float.NegativeInfinity;

        if (emergenceRoutine != null)
        {
            StopCoroutine(emergenceRoutine);
        }

        emergenceRoutine = StartCoroutine(Emerge(shadowInstance.transform, shadowFollower, walkablePoint));
        return true;
    }

    public void Recall()
    {
        if (emergenceRoutine != null)
        {
            StopCoroutine(emergenceRoutine);
            emergenceRoutine = null;
        }

        if (shadowInstance != null)
        {
            if (shadowFollower != null)
            {
                shadowFollower.Fell -= Recall;
            }

            DestroyShadowObject(shadowInstance);
        }

        shadowInstance = null;
        shadowFollower = null;
        pendingCommands.Clear();
        delayedCommands.Clear();
        lastScheduledExecutionTime = float.NegativeInfinity;
    }

    public void SetDelay(float seconds)
    {
        delaySeconds = StopwatchController.ResolveSelectedDelay(seconds);
    }

    public string CaptureActorConfiguration()
    {
        return JsonUtility.ToJson(new ShadowCheckpointConfiguration
        {
            hasShadow = HasShadow,
            position = HasShadow ? shadowInstance.transform.position : Vector3.zero,
            rotation = HasShadow ? shadowInstance.transform.rotation : Quaternion.identity,
            delaySeconds = delaySeconds
        });
    }

    public void RestoreActorConfiguration(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        var configuration = JsonUtility.FromJson<ShadowCheckpointConfiguration>(json);
        SetDelay(configuration.delaySeconds);
        if (!configuration.hasShadow)
        {
            Recall();
            return;
        }

        if (!TrySpawn(configuration.position))
        {
            return;
        }

        if (emergenceRoutine != null)
        {
            StopCoroutine(emergenceRoutine);
            emergenceRoutine = null;
        }

        shadowInstance.transform.SetPositionAndRotation(configuration.position, configuration.rotation);
        shadowFollower.enabled = true;
    }

    public static Vector3 GetEmergenceStart(Vector3 walkablePoint, float depth)
    {
        return walkablePoint - Vector3.up * depth;
    }

    private void QueueCommand(PlayerCommand command)
    {
        if (!HasShadow || command.Type == PlayerCommandType.Move)
        {
            return;
        }

        if (delaySeconds >= StopwatchController.ClickThresholdSeconds)
        {
            var executeAt = Mathf.Max(CurrentTime + delaySeconds, lastScheduledExecutionTime);
            delayedCommands.Enqueue(new ScheduledCommand(command, executeAt));
            lastScheduledExecutionTime = executeAt;
            return;
        }

        pendingCommands.Enqueue(command);
    }

    private IEnumerator Emerge(Transform spawnedShadow, ShadowFollower spawnedFollower, Vector3 walkablePoint)
    {
        spawnedFollower.enabled = false;
        var startPoint = spawnedShadow.position;
        var elapsed = 0f;

        while (elapsed < emergenceDuration)
        {
            elapsed += Time.deltaTime;
            spawnedShadow.position = Vector3.Lerp(startPoint, walkablePoint, Mathf.Clamp01(elapsed / emergenceDuration));
            yield return null;
        }

        spawnedShadow.position = walkablePoint;
        spawnedFollower.enabled = true;
        emergenceRoutine = null;
    }

    private void OnPlayerTravel(Vector3 delta)
    {
        if (shadowFollower != null && shadowFollower.isActiveAndEnabled)
        {
            shadowFollower.ApplyDisplacement(delta);
        }
    }

    private float CurrentTime => timeProvider != null ? timeProvider() : Time.time;

    private void Execute(PlayerCommand command)
    {
        if (shadowFollower == null || command.Type == PlayerCommandType.Move)
        {
            return;
        }

        var interactables = Physics.OverlapSphere(
            shadowInstance.transform.position + Vector3.up * 0.9f,
            interactionRadius,
            ~0,
            QueryTriggerInteraction.Collide);

        foreach (var interactableCollider in interactables)
        {
            var interactable = interactableCollider.GetComponentInParent<IActorInteractable>();
            if (interactable != null && interactable.TryInteract(ActorKind.Shadow))
            {
                return;
            }
        }
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        if (layer < 0)
        {
            return;
        }

        target.layer = layer;
        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static void DestroyShadowObject(GameObject target)
    {
        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    private readonly struct ScheduledCommand
    {
        public ScheduledCommand(PlayerCommand command, float executeAt)
        {
            Command = command;
            ExecuteAt = executeAt;
        }

        public PlayerCommand Command { get; }
        public float ExecuteAt { get; }
    }

    [Serializable]
    private struct ShadowCheckpointConfiguration
    {
        public bool hasShadow;
        public Vector3 position;
        public Quaternion rotation;
        public float delaySeconds;
    }
}
