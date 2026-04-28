using UnityEngine;

public class RepairPuzzleInteractable : MonoBehaviour, ISaveable
{
    [Header("Save")]
    [SerializeField] private string saveID;

    [Header("Puzzle Config")]
    public bool useSpecificScene = false;
    public string specificSceneName;
    public RepairPuzzleDifficulty difficulty = RepairPuzzleDifficulty.Difficulty1;

    [Header("Estado")]
    public bool completed;
    public bool blockIfCompleted = false;

    [Header("Fail Alert")]
    public float failAlertRadius = 12f;
    public LayerMask enemyLayer = ~0;
    public AudioSource failAudio;

    private void Start()
    {
        Debug.Log($"[Puzzle] {saveID} | completed={completed}");
    }

    public void StartRepairPuzzle()
    {
        if (blockIfCompleted && completed)
        {
            Debug.Log($"[Puzzle] {saveID} bloqueado — já completado.");
            return;
        }

        if (RepairPuzzleManager.Instance == null)
        {
            Debug.LogError("RepairPuzzleManager não encontrado.");
            return;
        }

        if (useSpecificScene && !string.IsNullOrWhiteSpace(specificSceneName))
            RepairPuzzleManager.Instance.OpenSpecificPuzzle(specificSceneName, this);
        else
            RepairPuzzleManager.Instance.OpenPuzzle(difficulty, this);
    }

    public void OnPuzzleFinished(RepairPuzzleResult result)
    {
        if (result == RepairPuzzleResult.Success)
        {
            completed = true;
            Debug.Log($"[Puzzle] {saveID} concluído. Salvando...");

            if (SaveManager.Instance != null)
                SaveManager.Instance.SaveGame();
        }
        else
        {
            Debug.Log($"[Puzzle] {saveID} falhou.");
            HandlePuzzleFail();
        }
    }

    private void HandlePuzzleFail()
    {
        if (failAudio != null)
            failAudio.Play();

        Collider[] hits = Physics.OverlapSphere(transform.position, failAlertRadius, enemyLayer, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Inimigo inimigo = hits[i].GetComponent<Inimigo>();

            if (inimigo == null)
                inimigo = hits[i].GetComponentInParent<Inimigo>();

            if (inimigo == null)
                inimigo = hits[i].GetComponentInChildren<Inimigo>();

            if (inimigo != null)
                inimigo.ForceChaseFromExternalAlert();
        }
    }

    public string GetSaveID() => saveID;

    public void SaveToData(SaveData data)
    {
        for (int i = 0; i < data.puzzles.Count; i++)
        {
            if (data.puzzles[i].id == saveID)
            {
                data.puzzles[i] = new PuzzleSaveRecord { id = saveID, completed = completed };
                Debug.Log($"[Puzzle] {saveID} atualizado no save | completed={completed}");
                return;
            }
        }

        data.puzzles.Add(new PuzzleSaveRecord { id = saveID, completed = completed });
        Debug.Log($"[Puzzle] {saveID} adicionado ao save | completed={completed}");
    }

    public void LoadFromSave(SaveData data)
    {
        for (int i = 0; i < data.puzzles.Count; i++)
        {
            if (data.puzzles[i].id == saveID)
            {
                completed = data.puzzles[i].completed;
                Debug.Log($"[Puzzle] {saveID} carregado | completed={completed}");
                return;
            }
        }

        Debug.Log($"[Puzzle] {saveID} NÃO encontrado no save.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, failAlertRadius);
    }
}