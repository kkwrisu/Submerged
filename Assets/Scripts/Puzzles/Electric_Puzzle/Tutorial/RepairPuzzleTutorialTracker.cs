using UnityEngine;

/// <summary>
/// Coloque este componente no mesmo GameObject (ou filho) do RepairPuzzleRuntime na cena do puzzle.
///
/// Ele:
///   1. Verifica no SaveData se o jogador já viu o tutorial deste tipo de puzzle.
///   2. Se for a primeira vez, bloqueia o Runtime e abre o overlay de tutorial.
///   3. Ao terminar (ou pular), desbloqueia o Runtime e salva que o tutorial foi visto.
///
/// DEPENDÊNCIAS ESPERADAS NO MESMO GAMEOBJECT:
///   - RepairPuzzleRuntime
///   - RepairPuzzleTutorialUI (pode estar em filho ou referenciado)
/// </summary>
[RequireComponent(typeof(RepairPuzzleRuntime))]
public class RepairPuzzleTutorialTracker : MonoBehaviour
{
    [Header("Tutorial")]
    [Tooltip("Dados dos slides do tutorial. Crie via Create > RepairPuzzle > TutorialData.")]
    public RepairPuzzleTutorialData tutorialData;

    [Tooltip("Referência ao componente de UI do tutorial na cena.")]
    public RepairPuzzleTutorialUI tutorialUI;

    [Tooltip("Se true, o tutorial sempre abre (útil para testar sem precisar limpar o save).")]
    public bool forceShowForDebug = false;

    // ── Estado interno ───────────────────────────────────────────────────────

    private RepairPuzzleRuntime runtime;
    public bool IsTutorialActive { get; private set; }

    // ── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        runtime = GetComponent<RepairPuzzleRuntime>();
    }

    /// <summary>
    /// Chamado pelo RepairPuzzleRuntime no Start(), após BuildMap e FindSpecialNodes.
    /// Retorna true se o tutorial foi aberto (Runtime deve ficar bloqueado até OnTutorialFinished).
    /// </summary>
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
        tutorialUI.Show(tutorialData, OnTutorialFinished);
        return true;
    }

    // ── Callbacks ────────────────────────────────────────────────────────────

    private void OnTutorialFinished()
    {
        IsTutorialActive = false;

        Debug.Log("[Tutorial] Tutorial concluído. Desbloqueando puzzle.");

        MarkTutorialSeen(tutorialData.tutorialID);

        // Desbloqueia o runtime para o jogador começar a jogar
        runtime.UnlockAfterTutorial();
    }

    // ── Save Integration ─────────────────────────────────────────────────────

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