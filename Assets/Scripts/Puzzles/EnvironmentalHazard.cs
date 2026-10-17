using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class EnvironmentalHazard : MonoBehaviour, IActorInteractable, ICheckpointState
{
    [SerializeField] private string checkpointId;
    [SerializeField] private BoxCollider hitVolume;
    [SerializeField] private Renderer warningRenderer;
    [SerializeField] private LayerMask enemyMask;
    [SerializeField, Min(0f)] private float telegraphSeconds = 0.6f;
    [SerializeField, Min(0f)] private float missCooldownSeconds = 1f;
    [SerializeField] private UnityEvent cleared = new UnityEvent();

    private Coroutine activation;
    private float nextReadyTime;

    public bool IsSpent { get; private set; }
    public bool IsArmed => activation != null;
    public UnityEvent Cleared => cleared;
    public string CheckpointId => string.IsNullOrWhiteSpace(checkpointId)
        ? $"{gameObject.scene.name}/{gameObject.name}" : checkpointId;

    private void Awake()
    {
        if (hitVolume == null) hitVolume = GetComponent<BoxCollider>();
        if (enemyMask.value == 0) enemyMask = LayerMask.GetMask("Enemy");
        if (warningRenderer != null) warningRenderer.enabled = false;
    }

    public void Configure(BoxCollider volume, Renderer warning, float warningSeconds,
        float cooldownSeconds)
    {
        hitVolume = volume;
        warningRenderer = warning;
        telegraphSeconds = Mathf.Max(0f, warningSeconds);
        missCooldownSeconds = Mathf.Max(0f, cooldownSeconds);
        enemyMask = LayerMask.GetMask("Enemy");
        if (warningRenderer != null) warningRenderer.enabled = false;
    }

    public bool TryInteract(ActorKind actor)
    {
        if (actor != ActorKind.Player || IsSpent || activation != null
            || Time.time < nextReadyTime || hitVolume == null)
            return false;
        activation = StartCoroutine(Activate());
        return true;
    }

    public void TriggerForTest(PatrolEnemy enemy)
    {
        Resolve(enemy);
    }

    public string CaptureState() => JsonUtility.ToJson(new SavedState
    {
        spent = IsSpent,
        cooldownRemaining = Mathf.Max(0f, nextReadyTime - Time.time)
    });

    public void RestoreState(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        if (activation != null)
        {
            StopCoroutine(activation);
            activation = null;
        }
        if (warningRenderer != null) warningRenderer.enabled = false;
        var state = JsonUtility.FromJson<SavedState>(json);
        IsSpent = state.spent;
        nextReadyTime = Time.time + Mathf.Max(0f, state.cooldownRemaining);
    }

    private IEnumerator Activate()
    {
        var elapsed = 0f;
        while (elapsed < telegraphSeconds)
        {
            elapsed += Time.deltaTime;
            if (warningRenderer != null) warningRenderer.enabled = Mathf.FloorToInt(elapsed * 12f) % 2 == 0;
            yield return null;
        }
        if (warningRenderer != null) warningRenderer.enabled = false;
        PatrolEnemy enemy = null;
        foreach (var candidate in Physics.OverlapBox(
            hitVolume.transform.TransformPoint(hitVolume.center),
            Vector3.Scale(hitVolume.size * 0.5f, hitVolume.transform.lossyScale),
            hitVolume.transform.rotation, enemyMask, QueryTriggerInteraction.Collide))
        {
            enemy = candidate.GetComponentInParent<PatrolEnemy>();
            if (enemy != null && !enemy.IsCleared) break;
        }
        Resolve(enemy);
        activation = null;
        if (!IsSpent) nextReadyTime = Time.time + missCooldownSeconds;
    }

    private void Resolve(PatrolEnemy enemy)
    {
        if (IsSpent || enemy == null || enemy.IsCleared) return;
        enemy.Clear();
        IsSpent = true;
        cleared?.Invoke();
    }

    private void OnDisable()
    {
        if (activation != null)
        {
            StopCoroutine(activation);
            activation = null;
        }
        if (warningRenderer != null) warningRenderer.enabled = false;
    }

    [System.Serializable]
    private struct SavedState
    {
        public bool spent;
        public float cooldownRemaining;
    }
}
