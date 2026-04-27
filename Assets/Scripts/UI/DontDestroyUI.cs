using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance;

    [Header("Cenas onde a UI deve ficar oculta")]
    public string[] hiddenInScenes = { "MainMenu", "MinigameScene" };

    [Header("Elementos que a UI controla")]
    public GameObject[] uiElements;

    private bool[] originalActiveStates;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        originalActiveStates = new bool[uiElements.Length];
        for (int i = 0; i < uiElements.Length; i++)
        {
            if (uiElements[i] != null)
                originalActiveStates[i] = uiElements[i].activeSelf;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool shouldHide = System.Array.Exists(hiddenInScenes, s => s == scene.name);

        for (int i = 0; i < uiElements.Length; i++)
        {
            if (uiElements[i] == null) continue;

            uiElements[i].SetActive(shouldHide ? false : originalActiveStates[i]);
        }
    }
}