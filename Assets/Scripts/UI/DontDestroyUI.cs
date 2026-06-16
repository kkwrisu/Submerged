using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance;

    [Header("Cenas onde a UI deve ficar oculta")]
    public string[] hiddenInScenes = { "MainMenu", "MinigameScene" };

    [Header("Ativados em gameplay, desativados nas cenas ocultas")]
    public GameObject[] uiElements;

    [Header("Sempre desativados (controlados por outros scripts)")]
    public GameObject[] managedElsewhere;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        Debug.Log($"[GameUI] Awake chamado. Instance existe: {Instance != null}");

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameUI] Duplicata destruída.");
            Destroy(gameObject);
            return;
        }

        transform.SetParent(null);
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("[GameUI] Registrado no SceneManager.");

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshForScene(scene.name);
    }

    public void RefreshForScene(string sceneName)
    {
        bool shouldHide = System.Array.Exists(hiddenInScenes, s => s == sceneName);
        Debug.Log($"[GameUI] RefreshForScene: {sceneName} | shouldHide: {shouldHide}");

        // Começa invisível em cenas de gameplay — cutscene ou ShowImmediate vão reativar
        canvasGroup.alpha = shouldHide ? 0f : 1f;

        foreach (var el in uiElements)
        {
            if (el != null)
            {
                el.SetActive(!shouldHide);
                Debug.Log($"[GameUI] {el.name} -> {el.activeSelf}");
            }
            else
            {
                Debug.LogError("[GameUI] elemento nulo no uiElements!");
            }
        }

        foreach (var el in managedElsewhere)
        {
            if (el != null)
                el.SetActive(false);
        }
    }

    public void ShowImmediate()
    {
        canvasGroup.alpha = 1f;
    }

    public IEnumerator FadeIn(float duration)
    {
        canvasGroup.alpha = 0f;

        if (duration <= 0f)
        {
            canvasGroup.alpha = 1f;
            yield break;
        }

        // Piscadas iniciais — simula boot
        int flickerCount = 4;
        float flickerSpeed = duration * 0.08f;

        for (int i = 0; i < flickerCount; i++)
        {
            canvasGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(flickerSpeed);
            canvasGroup.alpha = 0f;
            yield return new WaitForSecondsRealtime(flickerSpeed * (1f + i * 0.3f));
        }

        // Última piscada longa antes de estabilizar
        canvasGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(flickerSpeed * 2f);
        canvasGroup.alpha = 0f;
        yield return new WaitForSecondsRealtime(flickerSpeed);

        // Estabiliza com fade suave
        float elapsed = 0f;
        float stabilizeDuration = duration * 0.4f;

        while (elapsed < stabilizeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / stabilizeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }
}