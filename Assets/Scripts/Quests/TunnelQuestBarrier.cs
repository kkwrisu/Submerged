using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TunnelQuestBarrier : MonoBehaviour, ISaveable
{
    [Header("Identificação")]
    [Tooltip("ID único desse 'puzzle' de bloqueio, usado no save.")]
    public string questId;

    [Header("NPCs obrigatórios")]
    [Tooltip("Arraste aqui os Interactables (NPCs) que precisam ser conversados.")]
    public Interactable[] requiredNPCs;

    [Header("Mensagem de bloqueio")]
    [TextArea(2, 4)]
    public string blockedMessage = "Você precisa falar com todos antes de seguir.";
    public float messageDuration = 2.5f;

    [Header("Empurrão")]
    [Tooltip("Distância que o player é empurrado de volta.")]
    public float pushDistance = 1.5f;

    private HashSet<string> talkedIds = new HashSet<string>();
    private bool questCompleted = false;

    private Coroutine messageCoroutine;

    // ── Unity ─────────────────────────────────────────────────────────────

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnEnable()
    {
        Debug.Log($"[TunnelQuestBarrier] OnEnable em '{gameObject.name}' (questId={questId}) — inscrevendo no evento. talkedIds atuais: {string.Join(",", talkedIds)} | questCompleted={questCompleted}");
        DialogueManager.OnDialogueEnded += HandleDialogueEnded;
    }

    private void OnDisable()
    {
        Debug.Log($"[TunnelQuestBarrier] OnDisable em '{gameObject.name}' (questId={questId}) — desinscrevendo.");
        DialogueManager.OnDialogueEnded -= HandleDialogueEnded;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (questCompleted) return;
        if (!other.CompareTag("Player")) return;

        PushPlayerBack(other);
        ShowMessage();
    }

    // ── Empurrão / mensagem ──────────────────────────────────────────────

    private void PushPlayerBack(Collider playerCollider)
    {
        Transform player = playerCollider.transform;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            direction = -transform.forward;

        direction.Normalize();

        Vector3 newPosition = player.position + direction * pushDistance;

        CharacterController cc = player.GetComponent<CharacterController>();
        PlayerMovement pm = player.GetComponent<PlayerMovement>();

        if (cc != null) cc.enabled = false;

        player.position = newPosition;

        if (cc != null) cc.enabled = true;

        if (pm != null)
            pm.ResetMovementState();
    }

    private void ShowMessage()
    {
        if (DialogueManager.Instance == null) return;

        DialogueManager.Instance.ShowCutsceneDialogue(blockedMessage);

        if (messageCoroutine != null)
            StopCoroutine(messageCoroutine);

        messageCoroutine = StartCoroutine(HideMessageAfterDelay());
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDuration);

        if (DialogueManager.Instance != null)
            DialogueManager.Instance.HideCutsceneDialogue();

        messageCoroutine = null;
    }

    // ── Lógica de progresso ──────────────────────────────────────────────

    private void HandleDialogueEnded(Interactable interactable)
    {
        if (questCompleted) return;
        if (interactable == null || string.IsNullOrEmpty(interactable.interactableId)) return;

        Debug.Log($"[TunnelQuestBarrier] Evento recebido de '{interactable.name}' com ID '{interactable.interactableId}'");

        bool isRequired = false;
        for (int i = 0; i < requiredNPCs.Length; i++)
        {
            Debug.Log($"[TunnelQuestBarrier] Comparando com requiredNPCs[{i}] = '{requiredNPCs[i]?.name}' (ID: '{requiredNPCs[i]?.interactableId}')");
            if (requiredNPCs[i] == interactable)
            {
                isRequired = true;
                break;
            }
        }

        Debug.Log($"[TunnelQuestBarrier] isRequired = {isRequired} | talkedIds antes: {string.Join(",", talkedIds)}");

        if (!isRequired) return;

        if (talkedIds.Add(interactable.interactableId))
        {
            Debug.Log($"[TunnelQuestBarrier] Conversou com '{interactable.interactableId}' ({talkedIds.Count}/{requiredNPCs.Length})");
        }

        CheckCompletion();
    }

    private void CheckCompletion()
    {
        if (questCompleted) return;
        if (talkedIds.Count < requiredNPCs.Length) return;

        CompleteQuest();
    }

    private void CompleteQuest()
    {
        questCompleted = true;

        Debug.Log($"[TunnelQuestBarrier] Quest '{questId}' concluída — túnel liberado.");

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();
    }

    // ── ISaveable ─────────────────────────────────────────────────────────

    public string GetSaveID() => questId;

    public void SaveToData(SaveData data)
    {
        if (data.questProgress == null)
            data.questProgress = new List<QuestProgressRecord>();

        QuestProgressRecord record = new QuestProgressRecord
        {
            questId = questId,
            completed = questCompleted,
            talkedNpcIds = new List<string>(talkedIds)
        };

        int index = data.questProgress.FindIndex(r => r.questId == questId);
        if (index >= 0)
            data.questProgress[index] = record;
        else
            data.questProgress.Add(record);
    }

    public void LoadFromSave(SaveData data)
    {
        if (data.questProgress == null) return;

        int index = data.questProgress.FindIndex(r => r.questId == questId);
        if (index < 0) return;

        QuestProgressRecord record = data.questProgress[index];

        Debug.Log($"[TunnelQuestBarrier] LoadFromSave chamado para '{questId}'. completed={record.completed} | talkedNpcIds={(record.talkedNpcIds != null ? string.Join(",", record.talkedNpcIds) : "null")}");

        questCompleted = record.completed;

        talkedIds.Clear();
        if (record.talkedNpcIds != null)
        {
            foreach (var id in record.talkedNpcIds)
                talkedIds.Add(id);
        }
    }
}