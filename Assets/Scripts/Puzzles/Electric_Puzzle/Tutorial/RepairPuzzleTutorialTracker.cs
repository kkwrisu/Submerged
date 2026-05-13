using UnityEngine;

[RequireComponent(typeof(RepairPuzzleRuntime))]
public class RepairPuzzleTutorialTracker : MonoBehaviour
{
    [Header("Tutorial")]
    public RepairPuzzleTutorialData tutorialData;

    [Tooltip("Referência ao componente de UI do tutorial na cena.")]
    public RepairPuzzleTutorialUI tutorialUI;

    [Tooltip("Se true, o tutorial sempre abre (útil para testar sem precisar limpar o save).")]
    public bool forceShowForDebug = false;

    private RepairPuzzleRuntime runtime;
    public bool IsTutorialActive { get; private set; }

    private void Awake()
    {
        runtime = GetComponent<RepairPuzzleRuntime>();
    }

    public bool TryShowTutorial()
    {
        if (tutorialData == null)
        {
            Debug.Log("[Tutorial] Nenhum TutorialData configurado. Pulando tutorial.");
            return false;
        }

        if (tutorialUI == null)
        {
            Debug.LogWarning("[Tutorial] TutorialUI não referenciada. Pulando tutorial.");
            return false;
        }

        bool alreadySeen = !forceShowForDebug && HasSeenTutorial(tutorialData.tutorialID);

        if (alreadySeen)
        {
            Debug.Log("[Tutorial] Jogador já viu o tutorial '" + tutorialData.tutorialID + "'. Pulando.");
            return false;
        }

        Debug.Log("[Tutorial] Primeira vez — abrindo tutorial '" + tutorialData.tutorialID + "'.");
        IsTutorialActive = true;

        // Marca como visto imediatamente ao abrir
        MarkTutorialSeen(tutorialData.tutorialID);

        tutorialUI.Show(tutorialData, runtime, OnTutorialFinished);
        return true;
    }

    private void OnTutorialFinished()
    {
        IsTutorialActive = false;
        Debug.Log("[Tutorial] Tutorial concluído. Desbloqueando puzzle.");
        MarkTutorialSeen(tutorialData.tutorialID);
        runtime.UnlockAfterTutorial();
    }

    private bool HasSeenTutorial(string id)
    {
        if (SaveManager.Instance == null || SaveManager.Instance.CurrentSave == null)
            return false;

        return SaveDataTutorialExtensions.HasSeenTutorial(
            SaveManager.Instance.CurrentSave.seenTutorials, id
        );
    }

    private void MarkTutorialSeen(string id)
    {
        if (SaveManager.Instance == null || SaveManager.Instance.CurrentSave == null)
        {
            Debug.LogWarning("[Tutorial] SaveManager não encontrado. Tutorial não será salvo como visto.");
            return;
        }

        SaveDataTutorialExtensions.MarkTutorialSeen(
            SaveManager.Instance.CurrentSave.seenTutorials, id
        );

        SaveManager.Instance.SaveGame();
        Debug.Log("[Tutorial] Tutorial '" + id + "' marcado como visto no save.");
    }
}