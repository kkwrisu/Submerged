using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10)]
public class Alert_UI : MonoBehaviour
{
    [Header("Barras de Alerta")]
    public Image baseImage;
    public Image[] barImages = new Image[8];

    [Header("Flash Animation")]
    public float flashDuration = 0.08f;
    public int flashCount = 3;

    private int previousActiveBars = 0;
    private Coroutine[] flashCoroutines;

    // ── Unity ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        flashCoroutines = new Coroutine[barImages.Length];
        SceneManager.sceneLoaded += OnSceneLoaded;
        ConnectAlertSystem();
    }

    private void OnDestroy()
    {
        DisconnectAlertSystem();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DisconnectAlertSystem();
        ConnectAlertSystem();
        StartCoroutine(DelayedRefresh());
    }

    // ── Conexão ───────────────────────────────────────────────────────────────

    private void ConnectAlertSystem()
    {
        if (DungeonAlertSystem.Instance != null)
        {
            DungeonAlertSystem.Instance.onAlertChanged.RemoveListener(UpdateFill);
            DungeonAlertSystem.Instance.onAlertChanged.AddListener(UpdateFill);
            Refresh();
        }
        else
        {
            previousActiveBars = 0;
            SetActiveBars(0);
        }
    }

    private void DisconnectAlertSystem()
    {
        if (DungeonAlertSystem.Instance != null)
            DungeonAlertSystem.Instance.onAlertChanged.RemoveListener(UpdateFill);
    }

    private void UpdateFill(float value) => Refresh();

    private IEnumerator DelayedRefresh()
    {
        yield return null;
        yield return null;
        yield return null;
        Refresh();
    }

    // ── Barras ────────────────────────────────────────────────────────────────

    private void Refresh()
    {
        float alert = DungeonAlertSystem.Instance != null
            ? DungeonAlertSystem.Instance.currentAlert
            : 0f;

        int activeBars = Mathf.FloorToInt(alert / DungeonAlertSystem.AlertPerBar);
        activeBars = Mathf.Clamp(activeBars, 0, barImages.Length);

        // Desativa barras que sumiram + cancela flashes pendentes
        for (int i = activeBars; i < barImages.Length; i++)
        {
            if (flashCoroutines[i] != null)
            {
                StopCoroutine(flashCoroutines[i]);
                flashCoroutines[i] = null;
            }

            if (barImages[i] != null)
                barImages[i].gameObject.SetActive(false);
        }

        // Garante que barras já ativas continuem visíveis
        for (int i = 0; i < previousActiveBars && i < activeBars; i++)
            if (barImages[i] != null)
                barImages[i].gameObject.SetActive(true);

        // Anima com flash as barras recém-ativadas
        for (int i = previousActiveBars; i < activeBars; i++)
        {
            if (barImages[i] == null) continue;

            if (flashCoroutines[i] != null)
            {
                StopCoroutine(flashCoroutines[i]);
                flashCoroutines[i] = null;
            }

            flashCoroutines[i] = StartCoroutine(FlashBar(i));
        }

        previousActiveBars = activeBars;

        // Notifica o GlobalAlertEffect para recapturar os targets ativos
        var alertEffect = FindAnyObjectByType<GlobalAlertEffect>();
        if (alertEffect != null)
            alertEffect.RebuildTargets();
    }

    private IEnumerator FlashBar(int index)
    {
        GameObject bar = barImages[index].gameObject;

        for (int i = 0; i < flashCount; i++)
        {
            for (int j = 0; j < index; j++)
                if (barImages[j] != null)
                    barImages[j].gameObject.SetActive(true);

            bar.SetActive(true);
            yield return new WaitForSecondsRealtime(flashDuration);

            bar.SetActive(false);
            yield return new WaitForSecondsRealtime(flashDuration);
        }

        bar.SetActive(true);

        for (int j = 0; j < index; j++)
            if (barImages[j] != null)
                barImages[j].gameObject.SetActive(true);

        flashCoroutines[index] = null;
    }

    private void SetActiveBars(int count)
    {
        for (int i = 0; i < barImages.Length; i++)
            if (barImages[i] != null)
                barImages[i].gameObject.SetActive(i < count);
    }
}