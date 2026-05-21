using UnityEngine;

/// <summary>
/// Gerencia o estado dos dois tutoriais do Generator:
///   1. Approach  — exibido na primeira vez que o jogador se aproxima do gerador.
///   2. QTE       — exibido quando o primeiro QTE aparece no minigame.
///
/// Coloque este componente no mesmo GameObject do GeneratorInteractable (ou num pai).
/// </summary>
public class GeneratorTutorialTracker : MonoBehaviour
{
    [Header("Dados")]
    public GeneratorTutorialData tutorialData;

    [Header("UI")]
    public GeneratorTutorialUI tutorialUI;

    [Header("Debug")]
    [Tooltip("Se true, sempre exibe os tutoriais (ignora o save).")]
    public bool forceShowForDebug = false;

    // IDs internos para cada subtutorial
    private const string SUFFIX_APPROACH = "_approach";
    private const string SUFFIX_QTE = "_qte";

    public bool IsApproachTutorialActive { get; private set; }
    public bool IsQteTutorialActive { get; private set; }

    // ── Approach Tutorial ─────────────────────────────────────────────────────

    /// <summary>
    /// Chame quando o jogador entrar no trigger de proximidade do gerador.
    /// Retorna true se o tutorial foi aberto.
    /// </summary>
    public bool TryShowApproachTutorial(System.Action onFinished = null)
    {
        if (!CanShow(SUFFIX_APPROACH)) return false;

        IsApproachTutorialActive = true;
        MarkSeen(SUFFIX_APPROACH);

        tutorialUI.ShowApproach(tutorialData, () =>
        {
            IsApproachTutorialActive = false;
            Debug.Log("[GenTutorial] Approach tutorial concluído.");
            onFinished?.Invoke();
        });

        return true;
    }

    // ── QTE Tutorial ──────────────────────────────────────────────────────────

    /// <summary>
    /// Chame quando o primeiro QTE surgir.
    /// Passa o RectTransform do elemento de QTE a destacar.
    /// Retorna true se o tutorial foi aberto.
    /// </summary>
    public bool TryShowQteTutorial(RectTransform qteElement, System.Action onFinished = null)
    {
        if (!CanShow(SUFFIX_QTE)) return false;

        IsQteTutorialActive = true;
        MarkSeen(SUFFIX_QTE);

        // Pausa o jogo em tempo não-escalado para o tutorial
        Time.timeScale = 0f;

        tutorialUI.ShowQTE(tutorialData, qteElement, () =>
        {
            IsQteTutorialActive = false;
            Time.timeScale = 1f;
            Debug.Log("[GenTutorial] QTE tutorial concluído. Retomando jogo.");
            onFinished?.Invoke();
        });

        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool CanShow(string suffix)
    {
        if (tutorialData == null)
        {
            Debug.LogWarning("[GenTutorial] TutorialData não configurado.");
            return false;
        }
        if (tutorialUI == null)
        {
            Debug.LogWarning("[GenTutorial] TutorialUI não referenciada.");
            return false;
        }

        string fullID = tutorialData.tutorialID + suffix;

        if (!forceShowForDebug && HasSeenTutorial(fullID))
        {
            Debug.Log($"[GenTutorial] '{fullID}' já visto. Pulando.");
            return false;
        }

        return true;
    }

    private bool HasSeenTutorial(string id)
    {
        if (SaveManager.Instance?.CurrentSave == null) return false;
        return SaveDataTutorialExtensions.HasSeenTutorial(
            SaveManager.Instance.CurrentSave.seenTutorials, id);
    }

    private void MarkSeen(string suffix)
    {
        if (SaveManager.Instance?.CurrentSave == null)
        {
            Debug.LogWarning("[GenTutorial] SaveManager não encontrado.");
            return;
        }

        string fullID = tutorialData.tutorialID + suffix;
        SaveDataTutorialExtensions.MarkTutorialSeen(
            SaveManager.Instance.CurrentSave.seenTutorials, fullID);
        SaveManager.Instance.SaveGame();
        Debug.Log($"[GenTutorial] '{fullID}' marcado como visto.");
    }
}