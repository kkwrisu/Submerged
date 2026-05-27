using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [System.Serializable]
    public class DialogueChoice
    {
        public GameObject choiceButtonPrefab;
        public Sprite normalSprite;
        public Sprite hoverSprite;
        public int nextNodeIndex = -1;
        public bool closeDialogue = false;
        public bool triggerAction = false;
        public string actionDebugMessage;
        public UnityEvent onChoiceSelected;

        [Header("Elevator (opcional)")]
        public string sceneNameToCheck;
    }

    [System.Serializable]
    public class DialogueNode
    {
        [TextArea(2, 5)]
        public string text;
        public int nextNodeIndex = -1;
        public bool endDialogueHere = false;
        public DialogueChoice[] choices;
    }

    [Header("Dialogue")]
    public DialogueNode[] dialogueNodes;

    [Header("Action ao interagir (opcional)")]
    public bool triggerActionOnInteract;
    public string interactActionDebugMessage;
    public UnityEvent onInteract;

    public virtual void Interact()
    {
        Debug.Log("Interactable.Interact() chamado em: " + gameObject.name);

        if (triggerActionOnInteract)
            Debug.Log(interactActionDebugMessage);

        onInteract?.Invoke();

        if (dialogueNodes == null || dialogueNodes.Length == 0)
            return;

        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager.Instance está NULL.");
            return;
        }

        Debug.Log("Iniciando diálogo...");
        DialogueManager.Instance.StartDialogue(this);
    }
}