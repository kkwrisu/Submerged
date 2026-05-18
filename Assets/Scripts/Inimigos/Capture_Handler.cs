using System.Collections;
using TMPro;
using UnityEngine;

public class CaptureHandler : MonoBehaviour
{
    public static CaptureHandler Instance;

    [Header("Timing")]
    public float blackScreenDuration = 2.5f;

    [Header("Capture Text")]
    public TextMeshProUGUI captureText;
    public string captureMessage = "Você foi capturado";
    public float textFadeInDuration = 0.5f;
    public float textHoldDuration = 1f;
    public float textFadeOutDuration = 0.5f;

    private bool isHandlingCapture = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (captureText != null)
        {
            captureText.text = captureMessage;
            Color c = captureText.color;
            c.a = 0f;
            captureText.color = c;
            captureText.gameObject.SetActive(false);
        }
    }

    public void HandleCapture()
    {
        if (isHandlingCapture)
            return;

        StartCoroutine(CaptureRoutine());
    }

    private IEnumerator CaptureRoutine()
    {
        isHandlingCapture = true;

        // Fade para preto primeiro
        yield return StartCoroutine(FadeOut());

        // Tela preta — teleporta o player sem ele ser visto
        if (SaveManager.Instance != null)
            SaveManager.Instance.RespawnPlayerAtCheckpoint();

        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
            playerMovement.ResetAllStates();

        // Mostra o texto com fade
        if (captureText != null)
        {
            captureText.gameObject.SetActive(true);
            yield return StartCoroutine(FadeText(0f, 1f, textFadeInDuration));
            yield return new WaitForSecondsRealtime(textHoldDuration);
            yield return StartCoroutine(FadeText(1f, 0f, textFadeOutDuration));
            captureText.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSecondsRealtime(blackScreenDuration);
        }

        yield return new WaitForSecondsRealtime(0.1f);

        // Volta a mostrar a cena
        yield return StartCoroutine(FadeIn());

        isHandlingCapture = false;
    }

    private IEnumerator FadeText(float startAlpha, float endAlpha, float duration)
    {
        if (captureText == null) yield break;

        float elapsed = 0f;
        Color c = captureText.color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(startAlpha, endAlpha, Mathf.Clamp01(elapsed / duration));
            captureText.color = c;
            yield return null;
        }

        c.a = endAlpha;
        captureText.color = c;
    }

    private IEnumerator FadeOut()
    {
        SceneTransition st = SceneTransition.Instance;
        if (st == null || st.fadeImage == null) yield break;

        st.fadeImage.raycastTarget = true;

        float elapsed = 0f;
        Color c = st.fadeImage.color;

        while (elapsed < st.fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(elapsed / st.fadeDuration);
            st.fadeImage.color = c;
            yield return null;
        }

        c.a = 1f;
        st.fadeImage.color = c;
    }

    private IEnumerator FadeIn()
    {
        SceneTransition st = SceneTransition.Instance;
        if (st == null || st.fadeImage == null) yield break;

        float elapsed = 0f;
        Color c = st.fadeImage.color;

        while (elapsed < st.fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(1f - (elapsed / st.fadeDuration));
            st.fadeImage.color = c;
            yield return null;
        }

        c.a = 0f;
        st.fadeImage.color = c;
        st.fadeImage.raycastTarget = false;
    }
}