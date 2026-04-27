using UnityEngine;
using UnityEngine.Events;

public class DungeonAlertSystem : MonoBehaviour
{
    public static DungeonAlertSystem Instance;

    [Header("Alert Level")]
    [Range(0f, 100f)] public float currentAlert = 0f;
    public float maxAlert = 100f;

    [Header("Decay")]
    public bool alertDecaysOverTime = false;
    public float decayPerSecond = 2f;

    [Header("Gameplay Multipliers")]
    public float minSpeedMultiplier = 1f;
    public float maxSpeedMultiplier = 1.75f;

    public float minTimeMultiplier = 1f;
    public float maxTimeMultiplier = 2f;

    [Header("Events")]
    public UnityEvent<float> onAlertChanged;

    public float AlertNormalized => Mathf.Clamp01(currentAlert / maxAlert);
    public float SpeedMultiplier => Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier, AlertNormalized);
    public float TimeMultiplier => Mathf.Lerp(minTimeMultiplier, maxTimeMultiplier, AlertNormalized);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // 🔥 IMPORTANTE
    }

    private void Update()
    {
        if (!alertDecaysOverTime)
            return;

        if (currentAlert > 0f)
        {
            currentAlert -= decayPerSecond * Time.deltaTime;
            currentAlert = Mathf.Clamp(currentAlert, 0f, maxAlert);
            onAlertChanged?.Invoke(currentAlert);
        }
    }

    public void AddAlert(float amount)
    {
        if (amount <= 0f)
            return;

        currentAlert += amount;
        currentAlert = Mathf.Clamp(currentAlert, 0f, maxAlert);

        onAlertChanged?.Invoke(currentAlert);

        Debug.Log($"[DungeonAlertSystem] Alerta aumentado. Valor atual: {currentAlert}%");
    }

    public void SetAlert(float value)
    {
        currentAlert = Mathf.Clamp(value, 0f, maxAlert);
        onAlertChanged?.Invoke(currentAlert);
    }

    public void ResetAlert()
    {
        currentAlert = 0f;
        onAlertChanged?.Invoke(currentAlert);
    }

    public void RegisterMinigameFailure(float alertIncrease)
    {
        AddAlert(alertIncrease);
    }

    public void RegisterMinigameFailure()
    {
        AddAlert(20f);
    }
}