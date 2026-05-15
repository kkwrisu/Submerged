using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class Alert_UI : MonoBehaviour
{
    [Header("Barras de Alerta")]
    public Image baseImage;
    public Image[] barImages = new Image[8];

    [Header("Flash Animation")]
    public float flashDuration = 0.08f;
    public int flashCount = 3;

    [Header("Siren Effect")]
    public Color normalColor = Color.white;
    public Color fullAlertColor = new Color(1f, 0.4f, 0.4f, 1f);
    public float sirenSpeed = 2f;

    private bool isFull = false;
    private int previousActiveBars = 0;

    private Coroutine sirenCoroutine;
    private Coroutine[] flashCoroutines;

    private void Awake()
    {
        flashCoroutines = new Coroutine[barImages.Length];
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        ResetValueIfNoSystem();
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
        ResetValueIfNoSystem();
        ConnectAlertSystem();
    }

    private void ConnectAlertSystem()
    {
        if (DungeonAlertSystem.Instance != null)
        {
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

    private void ResetValueIfNoSystem()
    {
        if (DungeonAlertSystem.Instance == null)
        {
            previousActiveBars = 0;
            SetActiveBars(0);
        }
    }

    private void UpdateFill(float value)
    {
        Refresh();
    }

    private void Refresh()
    {
        float alert = DungeonAlertSystem.Instance != null
            ? DungeonAlertSystem.Instance.currentAlert
            : 0f;

        int activeBars = Mathf.FloorToInt(
            alert / DungeonAlertSystem.AlertPerBar
        );

        activeBars = Mathf.Clamp(activeBars, 0, barImages.Length);

        bool nowFull = activeBars >= barImages.Length;

        // DESLIGA apenas barras acima do nível atual
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

        // GARANTE que barras anteriores permaneçam ligadas
        for (int i = 0; i < previousActiveBars && i < activeBars; i++)
        {
            if (barImages[i] != null)
                barImages[i].gameObject.SetActive(true);
        }

        // FLASH apenas das novas barras
        for (int i = previousActiveBars; i < activeBars; i++)
        {
            if (barImages[i] == null)
                continue;

            if (flashCoroutines[i] != null)
            {
                StopCoroutine(flashCoroutines[i]);
                flashCoroutines[i] = null;
            }

            flashCoroutines[i] = StartCoroutine(FlashBar(i));
        }

        // ALERTA MÁXIMO
        if (nowFull && !isFull)
        {
            isFull = true;

            if (sirenCoroutine != null)
                StopCoroutine(sirenCoroutine);

            sirenCoroutine = StartCoroutine(SirenEffect());
        }

        // SAIU DO ALERTA MÁXIMO
        if (!nowFull && isFull)
        {
            isFull = false;

            if (sirenCoroutine != null)
            {
                StopCoroutine(sirenCoroutine);
                sirenCoroutine = null;
            }

            ApplyTint(normalColor);
        }

        previousActiveBars = activeBars;
    }

    private IEnumerator FlashBar(int index)
    {
        GameObject bar = barImages[index].gameObject;

        for (int i = 0; i < flashCount; i++)
        {
            // antes de piscar, garante que as anteriores continuam ligadas
            for (int j = 0; j < index; j++)
            {
                if (barImages[j] != null)
                    barImages[j].gameObject.SetActive(true);
            }

            bar.SetActive(true);
            yield return new WaitForSecondsRealtime(flashDuration);

            bar.SetActive(false);
            yield return new WaitForSecondsRealtime(flashDuration);
        }

        bar.SetActive(true);

        // GARANTE novamente que as anteriores continuem ligadas
        for (int j = 0; j < index; j++)
        {
            if (barImages[j] != null)
                barImages[j].gameObject.SetActive(true);
        }

        flashCoroutines[index] = null;
    }

    private IEnumerator SirenEffect()
    {
        while (true)
        {
            float t = Mathf.PingPong(Time.unscaledTime * sirenSpeed, 1f);

            Color current = Color.Lerp(normalColor, fullAlertColor, t);

            ApplyTint(current);

            yield return null;
        }
    }

    private void SetActiveBars(int count)
    {
        for (int i = 0; i < barImages.Length; i++)
        {
            if (barImages[i] != null)
                barImages[i].gameObject.SetActive(i < count);
        }
    }

    private void ApplyTint(Color color)
    {
        if (baseImage != null)
            baseImage.color = color;

        for (int i = 0; i < barImages.Length; i++)
        {
            if (barImages[i] != null)
                barImages[i].color = color;
        }
    }
}