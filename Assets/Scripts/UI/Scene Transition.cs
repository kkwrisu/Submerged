using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance;

    [Header("Fade")]
    public Image fadeImage;
    public float fadeDuration = 0.4f;

    [Header("Loading UI")]
    public GameObject loadingPanel;
    public Slider loadingBar;
    public TextMeshProUGUI loadingText;
    public float minimumLoadingTime = 1.5f;
    public float loadingBarSmoothSpeed = 1.5f;
    public float finalFillSpeed = 2.5f;

    private bool isTransitioning;

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
        }

        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    public void TransitionToScene(string sceneName)
    {
        if (isTransitioning)
            return;

        StartCoroutine(DoTransition(sceneName));
    }

    public void TransitionToScene(string sceneName, string spawnId)
    {
        if (isTransitioning)
            return;

        if (SaveManager.Instance != null)
            SaveManager.Instance.SetPendingSpawn(sceneName, spawnId);

        StartCoroutine(DoTransition(sceneName));
    }

    private IEnumerator DoTransition(string sceneName)
    {
        isTransitioning = true;

        yield return Fade(0f, 1f);

        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        if (loadingText != null)
            loadingText.text = "Loading...";

        if (loadingBar != null)
            loadingBar.value = 0f;

        float loadingTimer = 0f;
        float visualProgress = 0f;
        float targetProgress = 0f;

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName);
        loadOp.allowSceneActivation = false;

        while (true)
        {
            loadingTimer += Time.unscaledDeltaTime;

            float realProgress = Mathf.Clamp01(loadOp.progress / 0.9f);

            float timeProgress = minimumLoadingTime > 0f
                ? Mathf.Clamp01(loadingTimer / minimumLoadingTime)
                : 1f;

            targetProgress = Mathf.Min(realProgress, timeProgress);
            visualProgress = Mathf.MoveTowards(
                visualProgress,
                targetProgress,
                loadingBarSmoothSpeed * Time.unscaledDeltaTime
            );

            if (loadingBar != null)
                loadingBar.value = visualProgress;

            bool finishedLoading = loadOp.progress >= 0.9f;
            bool finishedMinimumTime = loadingTimer >= minimumLoadingTime;
            bool visualReachedTarget = Mathf.Approximately(visualProgress, targetProgress);

            if (finishedLoading && finishedMinimumTime && visualReachedTarget)
                break;

            yield return null;
        }

        while (visualProgress < 1f)
        {
            visualProgress = Mathf.MoveTowards(
                visualProgress,
                1f,
                finalFillSpeed * Time.unscaledDeltaTime
            );

            if (loadingBar != null)
                loadingBar.value = visualProgress;

            yield return null;
        }

        loadOp.allowSceneActivation = true;

        while (!loadOp.isDone)
            yield return null;

        yield return null;

        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        yield return Fade(1f, 0f);

        isTransitioning = false;
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        if (fadeImage == null)
            yield break;

        fadeImage.raycastTarget = true;

        float elapsed = 0f;
        Color c = fadeImage.color;
        c.a = startAlpha;
        fadeImage.color = c;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            c.a = Mathf.Lerp(startAlpha, endAlpha, t);
            fadeImage.color = c;

            yield return null;
        }

        c.a = endAlpha;
        fadeImage.color = c;

        if (Mathf.Approximately(endAlpha, 0f))
            fadeImage.raycastTarget = false;
    }
}