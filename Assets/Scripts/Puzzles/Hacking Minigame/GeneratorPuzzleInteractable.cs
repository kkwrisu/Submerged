using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Coloque este componente no objeto de gerador da cena.
/// Herda de Interactable — o raycast do PlayerInteract já detecta automaticamente.
/// Não precisa mais de TriggerZone nem PlayerAdapter.
/// </summary>
public class GeneratorPuzzleInteractable : Interactable, ISaveable
{
    [Header("Save")]
    [SerializeField] private string saveID;

    [Header("Estado")]
    public bool completed = false;
    public bool blockIfCompleted = false;

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

    /// <summary>
    /// Chamado automaticamente pelo PlayerInteract ao pressionar a tecla de interagir.
    /// Inicia o puzzle — o hold/release é escutado no Update abaixo.
    /// </summary>
    public override void Interact()
    {
        if (blockIfCompleted && completed)
        {
            Debug.Log($"[GenPuzzle] {saveID} bloqueado — já completado.");
            return;
        }

        if (_runtime == null)
            _runtime = FindFirstObjectByType<GeneratorPuzzleRuntime>();

        if (_runtime == null)
        {
            Debug.LogError("[GenPuzzle] GeneratorPuzzleRuntime não encontrado na cena.");
            return;
        }

        _runtime.BeginInteract(this);
    }

    private void Update()
    {
        if (_runtime == null || completed) return;

        // Só processa hold/release se este gerador é o ativo no runtime
        if (!_runtime.IsCurrentInteractable(this)) return;

        bool held = Keyboard.current != null && Keyboard.current.eKey.isPressed;
        bool released = Keyboard.current != null && Keyboard.current.eKey.wasReleasedThisFrame;

        if (held)
            _runtime.HoldInteract(this);

        if (released)
            _runtime.StopInteract(this);
    }

    public void OnPuzzleCompleted()
    {
        completed = true;
        Debug.Log($"[GenPuzzle] {saveID} concluído. Salvando...");

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();
    }

    public void OnPuzzleFailed()
    {
        Debug.Log($"[GenPuzzle] {saveID} — QTE falhou.");

        if (failAudio != null)
            failAudio.Play();

        // Alerta inimigos próximos — mesmo sistema do RepairPuzzle
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

        if (DungeonAlertSystem.Instance != null)
            DungeonAlertSystem.Instance.RegisterMinigameFailure();
    }

    // ── ISaveable ──────────────────────────────────────────────────────────────

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