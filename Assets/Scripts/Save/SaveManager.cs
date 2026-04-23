using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [Header("Player")]
    public Transform player;

    private SaveData currentSave;

    private string pendingSpawnId;
    private string pendingSceneName;
    private bool hasPendingSpawn;

    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentSave = LoadFileOrCreateNew();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyPendingSpawnIfNeeded(scene.name);
        ApplySceneState();
        RestorePlayerAtCheckpointIfPossible();
    }

    public void RegisterPlayer(Transform playerTransform)
    {
        player = playerTransform;
    }

    public void SetCheckpoint(Vector3 position, float yRotation, string sceneName)
    {
        if (currentSave == null)
            currentSave = new SaveData();

        currentSave.checkpointPosition = new SerializableVector3(position);
        currentSave.checkpointYRotation = yRotation;
        currentSave.currentSceneName = sceneName;

        SaveGame();
    }

    public void SetPendingSpawn(string sceneName, string spawnId)
    {
        pendingSceneName = sceneName;
        pendingSpawnId = spawnId;
        hasPendingSpawn = !string.IsNullOrWhiteSpace(sceneName) && !string.IsNullOrWhiteSpace(spawnId);
    }

    private void ApplyPendingSpawnIfNeeded(string loadedSceneName)
    {
        if (!hasPendingSpawn)
            return;

        if (loadedSceneName != pendingSceneName)
            return;

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

        SaveGame();
        ClearPendingSpawn();
    }

    private void ClearPendingSpawn()
    {
        hasPendingSpawn = false;
        pendingSceneName = null;
        pendingSpawnId = null;
    }

    public void SaveGame()
    {
        if (currentSave == null)
            currentSave = new SaveData();

        currentSave.currentSceneName = SceneManager.GetActiveScene().name;

        currentSave.levers.Clear();
        currentSave.doors.Clear();
        currentSave.puzzles.Clear();

        MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        for (int i = 0; i < allBehaviours.Length; i++)
        {
            if (allBehaviours[i] is ISaveable saveable)
                saveable.SaveToData(currentSave);
        }

        WriteToDisk();
    }

    public void LoadGameFromDisk()
    {
        currentSave = LoadFileOrCreateNew();
    }

    public bool HasSave()
    {
        return File.Exists(SavePath);
    }

    public string GetSavedSceneName()
    {
        if (currentSave == null)
            return null;

        return currentSave.currentSceneName;
    }

    public void LoadSavedScene()
    {
        if (currentSave == null || string.IsNullOrWhiteSpace(currentSave.currentSceneName))
        {
            Debug.LogWarning("Nenhuma cena salva encontrada.");
            return;
        }

        SceneManager.LoadScene(currentSave.currentSceneName);
    }

    public void RespawnPlayerAtCheckpoint()
    {
        if (currentSave == null || player == null)
        {
            Debug.LogWarning("Não foi possível respawnar: save ou player ausente.");
            return;
        }

        CharacterController cc = player.GetComponent<CharacterController>();

        if (cc != null)
            cc.enabled = false;

        player.position = currentSave.checkpointPosition.ToVector3();
        player.rotation = Quaternion.Euler(0f, currentSave.checkpointYRotation, 0f);

        if (cc != null)
            cc.enabled = true;
    }

    private void RestorePlayerAtCheckpointIfPossible()
    {
        if (player == null || currentSave == null)
            return;

        if (string.IsNullOrWhiteSpace(currentSave.currentSceneName))
            return;

        if (SceneManager.GetActiveScene().name != currentSave.currentSceneName)
            return;

        RespawnPlayerAtCheckpoint();
    }

    private void ApplySceneState()
    {
        if (currentSave == null)
            return;

        MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        for (int i = 0; i < allBehaviours.Length; i++)
        {
            if (allBehaviours[i] is ISaveable saveable)
                saveable.LoadFromSave(currentSave);
        }
    }

    private SaveData LoadFileOrCreateNew()
    {
        if (!File.Exists(SavePath))
            return new SaveData();

        string json = File.ReadAllText(SavePath);

        if (string.IsNullOrWhiteSpace(json))
            return new SaveData();

        SaveData loaded = JsonUtility.FromJson<SaveData>(json);
        return loaded ?? new SaveData();
    }

    private void WriteToDisk()
    {
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