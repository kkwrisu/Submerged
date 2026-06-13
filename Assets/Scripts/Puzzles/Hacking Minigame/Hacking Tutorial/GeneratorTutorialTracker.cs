using UnityEngine;

public class GeneratorTutorialTracker : MonoBehaviour
{
    [Header("Dados")]
    public GeneratorTutorialData tutorialData;

    [Header("UI")]
    public GeneratorTutorialUI tutorialUI;

    [Header("Debug")]
    [Tooltip("Se true, sempre exibe os tutoriais (ignora o save).")]
    public bool forceShowForDebug = false;

    private const string SUFFIX_APPROACH = "_approach";
    private const string SUFFIX_QTE = "_qte";

    public bool IsApproachTutorialActive { get; private set; }
    public bool IsQteTutorialActive { get; private set; }

    // ── Approach Tutorial ─────────────────────────────────────────────────────

    public bool TryShowApproachTutorial(System.Action onFinished = null)
    {
        if (!CanShow(SUFFIX_APPROACH)) return false;

        IsApproachTutorialActive = true;
        MarkSeen(SUFFIX_APPROACH);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        tutorialUI.ShowApproach(tutorialData, () =>
        {
            IsApproachTutorialActive = false;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Debug.Log("[GenTutorial] Approach tutorial concluído.");
            onFinished?.Invoke();
        });

        return true;
    }

    // ── QTE Tutorial ──────────────────────────────────────────────────────────

    public bool TryShowQteTutorial(System.Action onFinished = null)
    {
        if (!CanShow(SUFFIX_QTE)) return false;

        IsQteTutorialActive = true;
        MarkSeen(SUFFIX_QTE);

        Time.timeScale = 0f;

        tutorialUI.ShowQTE(tutorialData, () =>
        {
            IsQteTutorialActive = false;
            Time.timeScale = 1f;

            Debug.Log("[GenTutorial] QTE tutorial concluído. Retomando jogo.");
            onFinished?.Invoke();
        });

        return true;
    }

    // ── Force Close (interrupção externa) ───────────────────────────────────

    /// <summary>
    /// Fecha o tutorial à força (ex: jogador abriu outro puzzle no meio).
    /// Restaura timeScale, cursor e flags sem chamar callbacks.
    /// </summary>
    public void ForceClose()
    {
        if (!IsApproachTutorialActive && !IsQteTutorialActive) return;

        IsApproachTutorialActive = false;
        IsQteTutorialActive = false;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (tutorialUI != null && tutorialUI.panelRoot != null)
            tutorialUI.panelRoot.SetActive(false);

        Debug.Log("[GenTutorial] ForceClose — tutorial fechado por interrupção externa.");
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