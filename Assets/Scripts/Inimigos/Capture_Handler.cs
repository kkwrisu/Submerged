using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CaptureHandler : MonoBehaviour
{
    public static CaptureHandler Instance;

    [Header("Timing")]
    public float blackScreenDuration = 2.5f;

    [Header("References")]
    public Image fadeImage;
    public Image captureImage;
    public float fadeDuration = 0.4f;

    [Header("Capture Timing")]
    public float imageFadeInDuration = 0.5f;
    public float imageHoldDuration = 1f;
    public float imageFadeOutDuration = 0.5f;

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

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = false;
            fadeImage.gameObject.SetActive(false);
        }

        if (captureImage != null)
        {
            Color c = captureImage.color;
            c.a = 0f;
            captureImage.color = c;
            captureImage.gameObject.SetActive(false);
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

        PlayerLook playerLook = FindFirstObjectByType<PlayerLook>();
        playerLook?.FreezeAndReset();

        yield return StartCoroutine(FadeOut());

        if (SaveManager.Instance != null)
            SaveManager.Instance.RespawnPlayerAtCheckpoint();

        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
            playerMovement.ResetAllStates();

        if (captureImage != null)
        {
            captureImage.gameObject.SetActive(true);
            yield return StartCoroutine(FadeImage(captureImage, 0f, 1f, imageFadeInDuration));
            yield return new WaitForSecondsRealtime(imageHoldDuration);
            yield return StartCoroutine(FadeImage(captureImage, 1f, 0f, imageFadeOutDuration));
            captureImage.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSecondsRealtime(blackScreenDuration);
        }

        yield return new WaitForSecondsRealtime(0.1f);

        yield return StartCoroutine(FadeIn());

        playerLook?.Unfreeze();

        isHandlingCapture = false;
    }

    private IEnumerator FadeImage(Image image, float startAlpha, float endAlpha, float duration)
    {
        if (image == null) yield break;

        float elapsed = 0f;
        Color c = image.color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(startAlpha, endAlpha, Mathf.Clamp01(elapsed / duration));
            image.color = c;
            yield return null;
        }

        c.a = endAlpha;
        image.color = c;
    }

    private IEnumerator FadeOut()
    {
        if (fadeImage == null) yield break;

        fadeImage.gameObject.SetActive(true);
        fadeImage.raycastTarget = true;

        yield return StartCoroutine(FadeImage(fadeImage, 0f, 1f, fadeDuration));
    }

    private IEnumerator FadeIn()
    {
        if (fadeImage == null) yield break;

        yield return StartCoroutine(FadeImage(fadeImage, 1f, 0f, fadeDuration));

        fadeImage.raycastTarget = false;
        fadeImage.gameObject.SetActive(false);
    }
}