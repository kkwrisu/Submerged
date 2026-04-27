using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GeneratorMinigame : MonoBehaviour
{

    [Header("References")]
    public GeneratorMinigameUI ui;

    [Header("Progress")]
    [Tooltip("Tempo em segundos para completar o gerador sem erros.")]
    public float completionTime = 80f;

    [Tooltip("Quanto a barra regride ao errar o QTE (0–1).")]
    public float failRegressAmount = 0.08f;

    [Header("QTE Timing")]
    [Tooltip("Intervalo mínimo entre QTEs (segundos).")]
    public float qteIntervalMin = 5f;

    [Tooltip("Intervalo máximo entre QTEs (segundos).")]
    public float qteIntervalMax = 14f;

    [Tooltip("Janela total em que o QTE fica visível (segundos).")]
    public float qteTotalWindow = 2.2f;

    [Tooltip("Janela de 'perfeito' no centro da animação (segundos).")]
    public float qteSuccessWindow = 0.45f;

    [Header("Fail Alert")]
    public float failAlertRadius = 14f;
    public LayerMask enemyLayer = ~0;
    public AudioSource failAudio;
    public AudioSource successSkillCheckAudio;

    [Header("Callbacks")]
    public UnityEngine.Events.UnityEvent onGeneratorCompleted;
    public UnityEngine.Events.UnityEvent onQTEFail;

    private bool isActive;
    private bool isCompleted;

    private float progress;
    private float qteTimer;
    private float nextQteDelay;

    // QTE ativo
    private bool qteActive;
    private float qteElapsed;

    private Coroutine qteRoutine;

    public void StartMinigame()
    {
        if (isCompleted) return;

        isActive = true;
        ui?.Show();
        ScheduleNextQTE();
    }

    public void PauseMinigame()
    {
        isActive = false;
    }

    public void ExitMinigame()
    {
        isActive = false;

        if (qteActive)
            TriggerQTEFail(alertEnemies: false);

        qteActive = false;
        ui?.Hide();
    }

    public void OnQTEInput()
    {
        if (!isActive || !qteActive) return;

        float halfWindow = qteSuccessWindow * 0.5f;
        float center = qteTotalWindow * 0.5f;
        float delta = Mathf.Abs(qteElapsed - center);

        if (delta <= halfWindow)
            TriggerQTESuccess();
        else
            TriggerQTEFail(alertEnemies: true);
    }

    private void Update()
    {
        if (!isActive || isCompleted) return;

        progress += Time.deltaTime / completionTime;
        progress = Mathf.Clamp01(progress);
        ui?.SetProgress(progress);

        if (progress >= 1f)
        {
            CompleteGenerator();
            return;
        }

        if (qteActive)
        {
            qteElapsed += Time.deltaTime;
            ui?.SetQTEProgress(qteElapsed / qteTotalWindow);

            if (qteElapsed >= qteTotalWindow)
            {
                TriggerQTEFail(alertEnemies: true);
            }
        }
        else
        {
            qteTimer += Time.deltaTime;

            if (qteTimer >= nextQteDelay)
                SpawnQTE();
        }
    }

    private void SpawnQTE()
    {
        qteActive = true;
        qteElapsed = 0f;
        ui?.ShowQTE();
    }

    private void TriggerQTESuccess()
    {
        qteActive = false;
        ui?.HideQTE(success: true);

        if (successSkillCheckAudio != null)
            successSkillCheckAudio.Play();

        ScheduleNextQTE();
    }

    private void TriggerQTEFail(bool alertEnemies)
    {
        qteActive = false;
        ui?.HideQTE(success: false);

        progress = Mathf.Max(0f, progress - failRegressAmount);
        ui?.SetProgress(progress);

        if (failAudio != null)
            failAudio.Play();

        if (alertEnemies)
            AlertNearbyEnemies();

        onQTEFail?.Invoke();
        ScheduleNextQTE();
    }

    private void ScheduleNextQTE()
    {
        qteTimer = 0f;
        nextQteDelay = Random.Range(qteIntervalMin, qteIntervalMax);
    }

    private void CompleteGenerator()
    {
        isCompleted = true;
        isActive = false;
        qteActive = false;

        ui?.PlayCompletionAnimation();
        onGeneratorCompleted?.Invoke();
    }

    private void AlertNearbyEnemies()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, failAlertRadius, enemyLayer, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Inimigo enemy = hits[i].GetComponent<Inimigo>()
                         ?? hits[i].GetComponentInParent<Inimigo>()
                         ?? hits[i].GetComponentInChildren<Inimigo>();

            if (enemy != null)
                enemy.ForceChaseFromExternalAlert();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, failAlertRadius);
    }

    public bool IsCompleted() => isCompleted;
    public bool IsActive() => isActive;
    public float GetProgress() => progress;
}