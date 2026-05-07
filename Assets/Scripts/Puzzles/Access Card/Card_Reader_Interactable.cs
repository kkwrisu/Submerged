using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Leitor de cartão de acesso. Coloque este componente no objeto físico do leitor.
/// Herda de Interactable — o raycast do PlayerInteract detecta automaticamente.
///
/// Quando o jogador interage:
///   - Se o nível do cartão for suficiente → abre a porta vinculada (ou dispara evento).
///   - Se não for → feedback de acesso negado.
///
/// SETUP:
///   1. Adicione este componente ao prefab/objeto do leitor de cartão.
///   2. Preencha o saveID com um ID único (ex: "card_reader_lab_01").
///   3. Configure requiredCardLevel com o nível mínimo necessário.
///   4. Vincule targetDoor à porta que deve ser aberta, OU use o evento onAccessGranted.
/// </summary>
public class CardReaderInteractable : Interactable, ISaveable
{
    [Header("Save")]
    [SerializeField] private string saveID;

    [Header("Acesso")]
    [Tooltip("Nível mínimo do cartão necessário para passar.")]
    public int requiredCardLevel = 2;

    [Header("Porta (opcional)")]
    [Tooltip("Porta Doors que será desbloqueada. Deixe vazio se usar onAccessGranted.")]
    public Doors targetDoor;
    [Tooltip("Índice da trava desta porta que este leitor controla.")]
    public int lockIndex = 0;

    [Header("Estado")]
    public bool unlocked = false;
    public bool blockIfUnlocked = true;

    [Header("Feedback Visual (opcional)")]
    public GameObject lockedVisual;    // ex: luz vermelha
    public GameObject unlockedVisual;  // ex: luz verde

    [Header("Audio (opcional)")]
    public AudioSource grantedAudio;
    public AudioSource deniedAudio;

    [Header("Eventos")]
    public UnityEvent onAccessGranted;   // chamado quando acesso é liberado
    public UnityEvent onAccessDenied;    // chamado quando acesso é negado

    // ── Unity ──────────────────────────────────────────────────────────────────

    private void Start()
    {
        RefreshVisuals();
        Debug.Log($"[CardReader] {saveID} | requiredLevel={requiredCardLevel} | unlocked={unlocked}");
    }

    // ── Interactable ───────────────────────────────────────────────────────────

    public override void Interact()
    {
        if (blockIfUnlocked && unlocked)
        {
            Debug.Log($"[CardReader] {saveID} — já desbloqueado.");
            return;
        }

        if (AccessCardManager.Instance == null)
        {
            Debug.LogError("[CardReader] AccessCardManager não encontrado na cena.");
            return;
        }

        if (AccessCardManager.Instance.HasAccess(requiredCardLevel))
        {
            GrantAccess();
        }
        else
        {
            DenyAccess();
        }
    }

    // ── Lógica interna ─────────────────────────────────────────────────────────

    private void GrantAccess()
    {
        unlocked = true;
        RefreshVisuals();

        if (grantedAudio != null) grantedAudio.Play();

        if (targetDoor != null)
            targetDoor.UnlockLock(lockIndex);

        onAccessGranted?.Invoke();

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();

        Debug.Log($"[CardReader] {saveID} — acesso concedido! (nível {AccessCardManager.Instance.CardLevel} >= {requiredCardLevel})");
    }

    private void DenyAccess()
    {
        if (deniedAudio != null) deniedAudio.Play();
        onAccessDenied?.Invoke();

        Debug.Log($"[CardReader] {saveID} — acesso NEGADO. Necessário nível {requiredCardLevel}, jogador tem {AccessCardManager.Instance.CardLevel}.");
    }

    private void RefreshVisuals()
    {
        if (lockedVisual != null) lockedVisual.SetActive(!unlocked);
        if (unlockedVisual != null) unlockedVisual.SetActive(unlocked);
    }

    // ── ISaveable ──────────────────────────────────────────────────────────────

    public string GetSaveID() => saveID;

    public void SaveToData(SaveData data)
    {
        // Reutiliza a lista de puzzles para não precisar de nova lista no SaveData
        for (int i = 0; i < data.puzzles.Count; i++)
        {
            if (data.puzzles[i].id == saveID)
            {
                data.puzzles[i] = new PuzzleSaveRecord { id = saveID, completed = unlocked };
                return;
            }
        }
        data.puzzles.Add(new PuzzleSaveRecord { id = saveID, completed = unlocked });
    }

    public void LoadFromSave(SaveData data)
    {
        for (int i = 0; i < data.puzzles.Count; i++)
        {
            if (data.puzzles[i].id == saveID)
            {
                unlocked = data.puzzles[i].completed;
                RefreshVisuals();
                return;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = unlocked ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);

        if (targetDoor != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetDoor.transform.position);
        }
    }
}