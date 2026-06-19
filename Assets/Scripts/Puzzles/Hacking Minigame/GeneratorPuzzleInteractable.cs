using UnityEngine;
using UnityEngine.InputSystem;

public class GeneratorPuzzleInteractable : Interactable, ISaveable
{
    [Header("Save")]
    [SerializeField] private string saveID;

    [Header("Estado")]
    public bool completed = false;
    public bool blockIfCompleted = false;

    [Header("Bloqueio por Repair Puzzle (opcional)")]
    [Tooltip("Se preenchido, este notebook só funciona após o repair puzzle indicado ser concluído.")]
    public RepairPuzzleInteractable requiredRepairPuzzle;
    [Tooltip("Diálogo exibido quando o notebook está bloqueado.")]
    public DialogueNode[] lockedDialogueNodes;

    [Header("Tutorial")]
    public GeneratorTutorialTracker tutorialTracker;

    [Header("Fail Alert")]
    public float failAlertRadius = 12f;
    public LayerMask enemyLayer = ~0;
    public AudioSource failAudio;

    private GeneratorPuzzleRuntime _runtime;

    private void Start()
    {
        if (_runtime == null)
            _runtime = FindFirstObjectByType<GeneratorPuzzleRuntime>();

        Debug.Log($"[GenPuzzle] {saveID} | completed={completed}");
    }

    public override void Interact()
    {
        if (blockIfCompleted && completed)
        {
            Debug.Log($"[GenPuzzle] {saveID} bloqueado — já completado.");
            return;
        }

        // Verifica se está bloqueado por repair puzzle
        if (requiredRepairPuzzle != null && !requiredRepairPuzzle.completed)
        {
            Debug.Log($"[GenPuzzle] {saveID} bloqueado — repair puzzle não concluído.");

            if (lockedDialogueNodes != null && lockedDialogueNodes.Length > 0 && DialogueManager.Instance != null)
            {
                var originalNodes = dialogueNodes;
                dialogueNodes = lockedDialogueNodes;
                DialogueManager.Instance.StartDialogue(this);
                dialogueNodes = originalNodes;
            }

            return;
        }

        if (_runtime == null)
            _runtime = FindFirstObjectByType<GeneratorPuzzleRuntime>();

        if (_runtime == null)
        {
            Debug.LogError("[GenPuzzle] GeneratorPuzzleRuntime não encontrado na cena.");
            return;
        }

        if (tutorialTracker != null)
        {
            bool tutorialShown = tutorialTracker.TryShowApproachTutorial();
            if (tutorialShown) return;
        }

        _runtime.BeginInteract(this);
    }

    private void Update()
    {
        if (_runtime == null || completed) return;
        if (!_runtime.IsCurrentInteractable(this)) return;

        bool held = Keyboard.current != null && Keyboard.current.eKey.isPressed;
        bool released = Keyboard.current != null && Keyboard.current.eKey.wasReleasedThisFrame;

        if (held) _runtime.HoldInteract(this);
        if (released) _runtime.StopInteract(this);
    }

    public void OnPuzzleCompleted()
    {
        completed = true;
        Debug.Log($"[GenPuzzle] {saveID} concluído.");

        if (AccessCardManager.Instance != null)
            AccessCardManager.Instance.UpgradeCard();
        else if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();
    }

    public void OnPuzzleFailed()
    {
        Debug.Log($"[GenPuzzle] {saveID} — QTE falhou.");

        if (failAudio != null)
            failAudio.Play();

        Collider[] hits = Physics.OverlapSphere(transform.position, failAlertRadius, enemyLayer, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Inimigo inimigo = hits[i].GetComponent<Inimigo>();
            if (inimigo == null) inimigo = hits[i].GetComponentInParent<Inimigo>();
            if (inimigo == null) inimigo = hits[i].GetComponentInChildren<Inimigo>();
            if (inimigo != null) inimigo.ForceChaseFromExternalAlert();
        }

        if (DungeonAlertSystem.Instance != null)
            DungeonAlertSystem.Instance.RegisterMinigameFailure();
    }

    public string GetSaveID() => saveID;

    public void SaveToData(SaveData data)
    {
        for (int i = 0; i < data.puzzles.Count; i++)
        {
            if (data.puzzles[i].id == saveID)
            {
                data.puzzles[i] = new PuzzleSaveRecord { id = saveID, completed = completed };
                return;
            }
        }
        data.puzzles.Add(new PuzzleSaveRecord { id = saveID, completed = completed });
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, failAlertRadius);
    }
}