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

    public void StartRepairPuzzle()
    {
        if (blockIfCompleted && completed)
            return;

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
            Debug.Log("Puzzle concluído: " + gameObject.name);

            if (SaveManager.Instance != null)
                SaveManager.Instance.SaveGame();
        }
        else
        {
            Debug.Log("Puzzle falhou: " + gameObject.name);
        }
    }

    public string GetSaveID()
    {
        return saveID;
    }

    public void SaveToData(SaveData data)
    {
        PuzzleSaveRecord record = new PuzzleSaveRecord
        {
            id = saveID,
            completed = completed
        };

        data.puzzles.Add(record);
    }

    public void LoadFromSave(SaveData data)
    {
        for (int i = 0; i < data.puzzles.Count; i++)
        {
            if (data.puzzles[i].id == saveID)
            {
                completed = data.puzzles[i].completed;
                return;
            }
        }
    }
}