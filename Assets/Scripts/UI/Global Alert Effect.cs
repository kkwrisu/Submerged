using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-5)]
public class GlobalAlertEffect : MonoBehaviour
{
    [Header("Exclusões — Graphics que NÃO devem piscar")]
    public Graphic[] exclude;

    [Header("Configuração do Piscar")]
    public float minAlpha = 0.2f;
    public float sirenSpeed = 2f;

    private Graphic[] targets;
    private Color[] originalColors;
    private bool isFull = false;
    private Coroutine sirenCoroutine;

    // ── Unity ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        RebuildTargets();
        Debug.Log($"[GlobalAlertEffect] Awake — {targets.Length} targets encontrados.");
    }

    private void OnEnable()
    {
        if (DungeonAlertSystem.Instance != null)
        {
            DungeonAlertSystem.Instance.onAlertChanged.AddListener(OnAlertChanged);
            Debug.Log("[GlobalAlertEffect] Listener registrado no DungeonAlertSystem.");

            OnAlertChanged(DungeonAlertSystem.Instance.currentAlert);
        }
        else
        {
            Debug.LogWarning("[GlobalAlertEffect] DungeonAlertSystem.Instance é null no OnEnable!");
            StartCoroutine(WaitForAlertSystem());
        }
    }

    private void OnDisable()
    {
        if (DungeonAlertSystem.Instance != null)
            DungeonAlertSystem.Instance.onAlertChanged.RemoveListener(OnAlertChanged);

        StopSiren();
    }

    private IEnumerator WaitForAlertSystem()
    {
        while (DungeonAlertSystem.Instance == null)
            yield return null;

        DungeonAlertSystem.Instance.onAlertChanged.AddListener(OnAlertChanged);
        OnAlertChanged(DungeonAlertSystem.Instance.currentAlert);
        Debug.Log("[GlobalAlertEffect] Listener registrado após espera.");
    }

    // ── Debug (Editor only) ───────────────────────────────────────────────────

#if UNITY_EDITOR
    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.leftBracketKey.wasPressedThisFrame)
        {
            if (DungeonAlertSystem.Instance != null)
            {
                DungeonAlertSystem.Instance.AddAlert(DungeonAlertSystem.AlertPerBar);
                Debug.Log($"[GlobalAlertEffect][EDITOR] Alerta forçado para {DungeonAlertSystem.Instance.currentAlert}%");
            }
        }
    }
#endif

    // ── Setup ─────────────────────────────────────────────────────────────────

    public void RebuildTargets()
    {
        // Restaura cores antes de recapturar, para não salvar alpha reduzido
        if (targets != null)
            for (int i = 0; i < targets.Length; i++)
                if (targets[i] != null)
                    targets[i].color = originalColors[i];

        var excludeSet = new HashSet<Graphic>(exclude ?? new Graphic[0]);
        var all = GetComponentsInChildren<Graphic>();
        var filtered = new List<Graphic>();

        foreach (var g in all)
            if (!excludeSet.Contains(g))
                filtered.Add(g);

        targets = filtered.ToArray();
        originalColors = new Color[targets.Length];

        for (int i = 0; i < targets.Length; i++)
            if (targets[i] != null)
                originalColors[i] = targets[i].color;

        Debug.Log($"[GlobalAlertEffect] RebuildTargets — {targets.Length} targets. Excluídos: {excludeSet.Count}");
        for (int i = 0; i < targets.Length; i++)
            Debug.Log($"  [{i}] {targets[i]?.name} ({targets[i]?.GetType().Name})");
    }

    // ── Lógica ────────────────────────────────────────────────────────────────

    private void OnAlertChanged(float value)
    {
        bool nowFull = value >= DungeonAlertSystem.Instance.maxAlert;
        Debug.Log($"[GlobalAlertEffect] OnAlertChanged: value={value} | nowFull={nowFull} | isFull={isFull}");

        if (nowFull && !isFull)
        {
            isFull = true;
            if (sirenCoroutine != null) StopCoroutine(sirenCoroutine);
            sirenCoroutine = StartCoroutine(SirenEffect());
            Debug.Log("[GlobalAlertEffect] SirenEffect INICIADO.");
        }
        else if (!nowFull && isFull)
        {
            StopSiren();
            Debug.Log("[GlobalAlertEffect] SirenEffect PARADO.");
        }
    }

    private void StopSiren()
    {
        isFull = false;

        if (sirenCoroutine != null)
        {
            StopCoroutine(sirenCoroutine);
            sirenCoroutine = null;
        }

        // Restaura cores originais
        for (int i = 0; i < targets.Length; i++)
            if (targets[i] != null)
                targets[i].color = originalColors[i];
    }

    private IEnumerator SirenEffect()
    {
        Debug.Log("[GlobalAlertEffect] SirenEffect coroutine rodando...");
        while (true)
        {
            float t = Mathf.PingPong(Time.time * sirenSpeed, 1f);

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                {
                    Color c = originalColors[i];
                    c.a = Mathf.Lerp(minAlpha, originalColors[i].a, t);
                    targets[i].color = c;
                }
            }

            yield return null;
        }
    }
}