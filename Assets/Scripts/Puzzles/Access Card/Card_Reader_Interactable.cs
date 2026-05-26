using System.Collections;
using UnityEngine;
using UnityEngine.Events;

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
    public GameObject lockedVisual;
    public GameObject unlockedVisual;

    [Header("Audio (opcional)")]
    public AudioSource grantedAudio;
    public AudioSource deniedAudio;

    [Header("Player (para travar movimento)")]
    public MonoBehaviour playerMovementScript;
    public MonoBehaviour playerLookScript;

    [Header("Eventos")]
    public UnityEvent onAccessGranted;
    public UnityEvent onAccessDenied;

    private bool _waitingForDeniedDialogue = false;

    private void Start()
    {
        RefreshVisuals();
        Debug.Log($"[CardReader] {saveID} | requiredLevel={requiredCardLevel} | unlocked={unlocked}");
    }

    public override void Interact()
    {
        if (blockIfUnlocked && unlocked)
        {
            Debug.Log($"[CardReader] {saveID} — já desbloqueado.");
            return;
        }

        if (_waitingForDeniedDialogue) return;

        if (AccessCardManager.Instance == null)
        {
            Debug.LogError("[CardReader] AccessCardManager não encontrado na cena.");
            return;
        }

        if (AccessCardManager.Instance.HasAccess(requiredCardLevel))
            GrantAccess();
        else
            DenyAccess();
    }

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

        StartCoroutine(ShowDeniedDialogue());
    }

    private IEnumerator ShowDeniedDialogue()
    {
        _waitingForDeniedDialogue = true;

        if (playerMovementScript != null) playerMovementScript.enabled = false;
        if (playerLookScript != null) playerLookScript.enabled = false;

        yield return null;

        _waitingForDeniedDialogue = false;

        if (playerMovementScript != null) playerMovementScript.enabled = true;
        if (playerLookScript != null) playerLookScript.enabled = true;

        if (DialogueManager.Instance == null) yield break;

        Interactable.DialogueNode[] nodes = new Interactable.DialogueNode[]
        {
            new Interactable.DialogueNode
            {
                text = $"Acesso negado. Nível de acesso {requiredCardLevel} necessário.",
                endDialogueHere = true
            }
        };

        Interactable temp = gameObject.AddComponent<Interactable>();
        temp.dialogueNodes = nodes;
        DialogueManager.Instance.StartDialogue(temp);
        Destroy(temp, 0.1f);
    }

    private void RefreshVisuals()
    {
        if (lockedVisual != null) lockedVisual.SetActive(!unlocked);
        if (unlockedVisual != null) unlockedVisual.SetActive(unlocked);
    }

    public string GetSaveID() => saveID;

    public void SaveToData(SaveData data)
    {
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