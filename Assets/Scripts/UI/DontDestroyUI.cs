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
}