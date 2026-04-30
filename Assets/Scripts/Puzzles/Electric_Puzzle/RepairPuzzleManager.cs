using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum RepairPuzzleDifficulty
{
    Difficulty1 = 1,
    Difficulty2 = 2,
    Difficulty3 = 3,
    Difficulty4 = 4
}

public enum RepairPuzzleResult
{
    None,
    Success,
    Fail
}

public class RepairPuzzleManager : MonoBehaviour
{
    public static RepairPuzzleManager Instance;

    [Header("Scene Pools")]
    public string[] difficulty1Scenes;
    public string[] difficulty2Scenes;
    public string[] difficulty3Scenes;
    public string[] difficulty4Scenes;

    [Header("Player Lock")]
    public MonoBehaviour playerMovement;
    public MonoBehaviour playerLook;
    public PlayerInteract playerInteract;
    public Camera playerCamera;
    public AudioListener playerAudioListener;

    [Header("Pause World")]
    public bool pauseGameWhilePuzzleIsOpen = true;

    private string loadedScene;
    private Scene previousScene;
    private RepairPuzzleInteractable currentInteractable;
    private bool isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void OpenPuzzle(RepairPuzzleDifficulty difficulty, RepairPuzzleInteractable interactable)
    {
        if (isOpen)
            return;

        string scene = GetRandomScene(difficulty);

        if (string.IsNullOrWhiteSpace(scene))
        {
            Debug.LogError("Nenhuma cena configurada para a dificuldade " + difficulty);
            return;
        }

        currentInteractable = interactable;
        StartCoroutine(OpenRoutine(scene));
    }

    public void OpenSpecificPuzzle(string sceneName, RepairPuzzleInteractable interactable)
    {
        if (isOpen)
            return;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("specificSceneName está vazio.");
            return;
        }

        currentInteractable = interactable;
        StartCoroutine(OpenRoutine(sceneName));
    }

    private IEnumerator OpenRoutine(string sceneName)
    {
        isOpen = true;
        loadedScene = sceneName;

        previousScene = SceneManager.GetActiveScene();

        LockPlayer(true);

        if (pauseGameWhilePuzzleIsOpen)
            Time.timeScale = 0f;

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!op.isDone)
            yield return null;

        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.IsValid())
            SceneManager.SetActiveScene(scene);

        Debug.Log("Cena do puzzle carregada: " + sceneName);
    }

    public void FinishPuzzle(RepairPuzzleResult result)
    {
        if (!isOpen)
            return;

        StartCoroutine(CloseRoutine(result));
    }

    private IEnumerator CloseRoutine(RepairPuzzleResult result)
    {
        if (previousScene.IsValid())
            SceneManager.SetActiveScene(previousScene);

        if (!string.IsNullOrWhiteSpace(loadedScene))
        {
            AsyncOperation op = SceneManager.UnloadSceneAsync(loadedScene);
            while (op != null && !op.isDone)
                yield return null;
        }

        loadedScene = null;
        isOpen = false;

        if (GameUI.Instance != null)
            GameUI.Instance.RefreshForScene(previousScene.name);

        if (pauseGameWhilePuzzleIsOpen)
            Time.timeScale = 1f;

        LockPlayer(false);

        if (currentInteractable != null)
        {
            currentInteractable.OnPuzzleFinished(result);
            currentInteractable = null;
        }
    }

    private void LockPlayer(bool locked)
    {
        if (playerMovement != null)
            playerMovement.enabled = !locked;

        if (playerLook != null)
            playerLook.enabled = !locked;

        if (playerInteract != null)
            playerInteract.enabled = !locked;

        if (playerCamera != null)
            playerCamera.enabled = !locked;

        if (playerAudioListener != null)
            playerAudioListener.enabled = !locked;

        Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = locked;
    }

    private string GetRandomScene(RepairPuzzleDifficulty difficulty)
    {
        string[] pool = null;

        switch (difficulty)
        {
            case RepairPuzzleDifficulty.Difficulty1: pool = difficulty1Scenes; break;
            case RepairPuzzleDifficulty.Difficulty2: pool = difficulty2Scenes; break;
            case RepairPuzzleDifficulty.Difficulty3: pool = difficulty3Scenes; break;
            case RepairPuzzleDifficulty.Difficulty4: pool = difficulty4Scenes; break;
        }

        if (pool == null || pool.Length == 0)
            return null;

        int validCount = 0;
        for (int i = 0; i < pool.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(pool[i]))
                validCount++;
        }

        if (validCount == 0)
            return null;

        int randomIndex;
        do
        {
            randomIndex = Random.Range(0, pool.Length);
        }
        while (string.IsNullOrWhiteSpace(pool[randomIndex]));

        return pool[randomIndex];
    }

    public bool IsPuzzleOpen()
    {
        return isOpen;
    }
}