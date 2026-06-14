using UnityEngine;

public class NPCInteractable : Interactable
{
    private NPCAnimator npcAnimator;

    private void Awake()
    {
        npcAnimator = GetComponent<NPCAnimator>();
    }

    public override void Interact()
    {
        if (triggerActionOnInteract)
        {
            Debug.Log(interactActionDebugMessage);
            onInteract?.Invoke();
        }

        if (dialogueNodes == null || dialogueNodes.Length == 0)
            return;

        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager.Instance está NULL.");
            return;
        }

        DialogueManager.Instance.StartDialogue(this, pauseGame: false);
        npcAnimator?.OnDialogueStart();
    }
}