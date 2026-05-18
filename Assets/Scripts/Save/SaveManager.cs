using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(10)]
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [Header("Player")]
    public Transform player;

    private SaveData currentSave;
    public SaveData CurrentSave => currentSave;

    private string pendingSpawnId;
    private string pendingSceneName;
    private bool hasPendingSpawn;

    // Cena de puzzle Additive ativa — ignorada pelo SaveGame.
    // Registre/desregistre via RegisterPuzzleScene / UnregisterPuzzleScene.
    private string activePuzzleScene = null;

    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    // ── Unity ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[SaveManager] Segunda instância detectada e destruída.");
            Destroy(gameObject);
            return;
        }

        Debug.Log("[SaveManager] Awake — inicializando instância principal.");
        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentSave = LoadFileOrCreateNew();

        if (currentSave != null)
        {
            Debug.Log($"[SaveManager] Save carregado. seenTutorials.Count={currentSave.seenTutorials.Count}");
            for (int i = 0; i < currentSave.seenTutorials.Count; i++)
                Debug.Log($"[SaveManager]   seenTutorial[{i}] = {currentSave.seenTutorials[i].tutorialID}");
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ── Puzzle Scene Registration ─────────────────────────────────────────────

    public void RegisterPuzzleScene(string sceneName)
    {
        activePuzzleScene = sceneName;
        Debug.Log($"[SaveManager] Cena de puzzle registrada (ignorada no SaveGame): {sceneName}");
    }

    public void UnregisterPuzzleScene()
    {
        Debug.Log($"[SaveManager] Cena de puzzle desregistrada: {activePuzzleScene}");
        activePuzzleScene = null;
    }

    // ── Scene Events ──────────────────────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SaveManager] OnSceneLoaded: {scene.name} | mode={mode} | seenTutorials.Count={currentSave?.seenTutorials?.Count}");

        ApplyPendingSpawnIfNeeded(scene.name);
        ApplySceneState();

        if (mode == LoadSceneMode.Single)
            RestorePlayerAtCheckpointIfPossible();
    }

    // ── Player ────────────────────────────────────────────────────────────────

    public void RegisterPlayer(Transform playerTransform)
    {
        player = playerTransform;
    }

    // ── Checkpoint ────────────────────────────────────────────────────────────

    public void SetCheckpoint(Vector3 position, float yRotation, string sceneName)
    {
        if (currentSave == null)
            currentSave = new SaveData();

        currentSave.checkpointPosition = new SerializableVector3(position);
        currentSave.checkpointYRotation = yRotation;
        currentSave.currentSceneName = sceneName;

        SaveGame();
    }

    // ── Pending Spawn ─────────────────────────────────────────────────────────

    public void SetPendingSpawn(string sceneName, string spawnId)
    {
        pendingSceneName = sceneName;
        pendingSpawnId = spawnId;
        hasPendingSpawn = !string.IsNullOrWhiteSpace(sceneName) && !string.IsNullOrWhiteSpace(spawnId);
    }

    private void ApplyPendingSpawnIfNeeded(string loadedSceneName)
    {
        if (!hasPendingSpawn) return;
        if (loadedSceneName != pendingSceneName) return;

        SceneSpawnPoint[] spawnPoints = FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None);
        SceneSpawnPoint selectedSpawn = null;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i].spawnId == pendingSpawnId)
            {
                selectedSpawn = spawnPoints[i];
                break;
            }
        }

        if (selectedSpawn == null)
        {
            Debug.LogWarning("Spawn point não encontrado: " + pendingSpawnId);
            ClearPendingSpawn();
            return;
        }

        if (currentSave == null)
            currentSave = new SaveData();

        currentSave.checkpointPosition = new SerializableVector3(selectedSpawn.transform.position);
        currentSave.checkpointYRotation = selectedSpawn.transform.eulerAngles.y;
        currentSave.currentSceneName = loadedSceneName;

        WriteToDisk();
        ClearPendingSpawn();
    }

    private void ClearPendingSpawn()
    {
        hasPendingSpawn = false;
        pendingSceneName = null;
        pendingSpawnId = null;
    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    public void SaveGame()
    {
        if (currentSave == null)
            currentSave = new SaveData();

        string sceneToRecord = GetMainSceneName();
        currentSave.currentSceneName = sceneToRecord;

        Debug.Log($"[SaveManager] SaveGame — cena='{sceneToRecord}' | seenTutorials.Count={currentSave.seenTutorials.Count} | activePuzzleScene='{activePuzzleScene}'");

        MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        for (int i = 0; i < allBehaviours.Length; i++)
        {
            if (allBehaviours[i] is ISaveable saveable)
            {
                // Ignora ISaveables da cena de puzzle Additive (estado temporário)
                if (!string.IsNullOrEmpty(activePuzzleScene))
                {
                    if (allBehaviours[i].gameObject.scene.name == activePuzzleScene)
                        continue;
                }

                saveable.SaveToData(currentSave);
            }
        }

        WriteToDisk();
    }

    private string GetMainSceneName()
    {
        if (string.IsNullOrEmpty(activePuzzleScene))
            return SceneManager.GetActiveScene().name;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.isLoaded && s.name != activePuzzleScene)
                return s.name;
        }

        return SceneManager.GetActiveScene().name;
    }

    public void LoadGameFromDisk()
    {
        currentSave = LoadFileOrCreateNew();
    }

    public bool HasSave() => File.Exists(SavePath);

    public string GetSavedSceneName() => currentSave?.currentSceneName;

    public void LoadSavedScene()
    {
        if (currentSave == null || string.IsNullOrWhiteSpace(currentSave.currentSceneName))
        {
            Debug.LogWarning("Nenhuma cena salva encontrada.");
            return;
        }

        SceneManager.LoadScene(currentSave.currentSceneName);
    }

    // ── Respawn ───────────────────────────────────────────────────────────────

    public void RespawnPlayerAtCheckpoint()
    {
        if (currentSave == null || player == null)
        {
            Debug.LogWarning("Não foi possível respawnar: save ou player ausente.");
            return;
        }

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.position = currentSave.checkpointPosition.ToVector3();
        player.rotation = Quaternion.Euler(0f, currentSave.checkpointYRotation, 0f);

        if (cc != null) cc.enabled = true;
    }

    private void RestorePlayerAtCheckpointIfPossible()
    {
        if (player == null || currentSave == null) return;
        if (string.IsNullOrWhiteSpace(currentSave.currentSceneName)) return;
        if (SceneManager.GetActiveScene().name != currentSave.currentSceneName) return;

        RespawnPlayerAtCheckpoint();
    }

    // ── Apply State ───────────────────────────────────────────────────────────

    private void ApplySceneState()
    {
        if (currentSave == null) return;

        Debug.Log($"[SaveManager] ApplySceneState — seenTutorials.Count={currentSave.seenTutorials.Count}");

        MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        for (int i = 0; i < allBehaviours.Length; i++)
        {
            if (allBehaviours[i] is ISaveable saveable)
                saveable.LoadFromSave(currentSave);
        }
    }

    // ── Disk I/O ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Lê o arquivo de save do disco.
    /// CORREÇÃO: JsonUtility.FromJson nunca retorna null, mas não inicializa
    /// listas na build compilada — EnsureListsInitialized() corrige isso.
    /// </summary>
    private SaveData LoadFileOrCreateNew()
    {
        if (!File.Exists(SavePath))
            return new SaveData();

        string json = File.ReadAllText(SavePath);

        if (string.IsNullOrWhiteSpace(json))
            return new SaveData();

        SaveData loaded = JsonUtility.FromJson<SaveData>(json);

        if (loaded == null)
            return new SaveData();

        // Garante que listas não fiquem null após desserialização na build
        loaded.EnsureListsInitialized();

        return loaded;
    }

    private void WriteToDisk()
    {
        Debug.Log($"[SaveManager] WriteToDisk — seenTutorials.Count={currentSave?.seenTutorials?.Count}");
        if (currentSave?.seenTutorials != null)
        {
            for (int i = 0; i < currentSave.seenTutorials.Count; i++)
                Debug.Log($"[SaveManager]   seenTutorial[{i}] = {currentSave.seenTutorials[i].tutorialID}");
        }

        string json = JsonUtility.ToJson(currentSave, true);
        File.WriteAllText(SavePath, json);
        Debug.Log("Save gravado em: " + SavePath);
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);

        currentSave = new SaveData();
        ClearPendingSpawn();
    }
}