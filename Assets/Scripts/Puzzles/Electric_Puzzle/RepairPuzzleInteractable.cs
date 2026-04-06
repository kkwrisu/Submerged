using UnityEngine;

public class RepairPuzzleInteractable : MonoBehaviour
{
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
        }
        else
        {
            Debug.Log("Puzzle falhou: " + gameObject.name);
        }
    }
}