using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    public static event Action<Interactable> OnDialogueEnded;

    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    [Header("Choices UI")]
    public Transform choicesContainer;

    [Header("UI Elements to Hide During Dialogue")]
    public GameObject[] uiElementsToHide;

    [Header("Gameplay Lock")]
    public GameObject crosshair;
    public MonoBehaviour playerMovementScript;
    public MonoBehaviour playerLookScript;
    public float inputBlockAfterStart = 0.15f;

    [Header("Typewriter")]
    public float typewriterSpeed = 40f;
    public AudioSource typewriterAudioSource;

    private Interactable.DialogueNode[] nodes;
    private int currentNodeIndex;

    private Interactable currentInteractable;

    private bool isActive = false;
    private bool waitingForChoice = false;
    private float nextInputAllowedTime = 0f;

    private Coroutine typewriterCoroutine;
    public bool IsTyping { get; private set; }

    private bool isCutsceneMode = false;
    private bool shouldPauseGame = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        HideAllChoiceButtons();
    }

    // -------------------------------------------------------------------------
    // Typewriter
    // -------------------------------------------------------------------------

    private void StartTypewriter(string text, AudioClip tickClip = null, float tickVolume = 0.3f, int tickEveryN = 2)
    {
        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);

        IsTyping = true;
        typewriterCoroutine = StartCoroutine(TypewriterRoutine(text, tickClip, tickVolume, tickEveryN));
    }

    private IEnumerator TypewriterRoutine(string text, AudioClip tickClip, float tickVolume, int tickEveryN)
    {
        dialogueText.text = "";
        float delay = 1f / typewriterSpeed;
        int charCount = 0;

        foreach (char c in text)
        {
            dialogueText.text += c;
            charCount++;

            if (tickClip != null && typewriterAudioSource != null && charCount % tickEveryN == 0)
                typewriterAudioSource.PlayOneShot(tickClip, tickVolume);

            yield return new WaitForSecondsRealtime(delay);
        }

        IsTyping = false;
    }

    public void SkipTypewriter(string fullText)
    {
        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);

        if (dialogueText != null)
            dialogueText.text = fullText;

        IsTyping = false;
    }

    // -------------------------------------------------------------------------
    // Reconexão de UI
    // -------------------------------------------------------------------------

    private void TryReconnectUI()
    {
        if (dialoguePanel == null)
        {
            var found = System.Array.Find(
                Resources.FindObjectsOfTypeAll<Transform>(),
                t => t.name == "DialoguePanel"
            );
            if (found != null) dialoguePanel = found.gameObject;
        }

        if (dialogueText == null && dialoguePanel != null)
            dialogueText = dialoguePanel.GetComponentInChildren<TextMeshProUGUI>(true);

        if (choicesContainer == null && dialoguePanel != null)
        {
            var found = System.Array.Find(
                Resources.FindObjectsOfTypeAll<Transform>(),
                t => t.name == "ChoicesContainer"
            );
            if (found != null) choicesContainer = found;
        }

        if (crosshair == null || playerMovementScript == null || playerLookScript == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (playerMovementScript == null)
                    playerMovementScript = player.GetComponent<PlayerMovement>();
                if (playerLookScript == null)
                    playerLookScript = player.GetComponentInChildren<PlayerLook>();
            }

            if (crosshair == null && GameUI.Instance != null)
            {
                var found = System.Array.Find(
                    Resources.FindObjectsOfTypeAll<Transform>(),
                    t => t.name == "Crosshair"
                );
                if (found != null) crosshair = found.gameObject;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Diálogo normal
    // -------------------------------------------------------------------------

    public void StartDialogue(Interactable interactable, bool pauseGame = true)
    {
        TryReconnectUI();

        if (interactable == null || interactable.dialogueNodes == null || interactable.dialogueNodes.Length == 0)
            return;

        if (dialoguePanel == null)
        {
            Debug.LogError("[DialogueManager] dialoguePanel é NULL.");
            return;
        }

        isCutsceneMode = false;
        shouldPauseGame = pauseGame;
        currentInteractable = interactable;
        nodes = interactable.dialogueNodes;
        currentNodeIndex = 0;
        isActive = true;
        waitingForChoice = false;
        nextInputAllowedTime = Time.unscaledTime + inputBlockAfterStart;

        dialoguePanel.SetActive(true);

        foreach (var element in uiElementsToHide)
            if (element != null) element.SetActive(false);

        if (crosshair != null) crosshair.SetActive(false);
        if (playerMovementScript != null) playerMovementScript.enabled = false;
        if (playerLookScript != null) playerLookScript.enabled = false;

        if (shouldPauseGame)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        ShowNode(currentNodeIndex);
    }

    // -------------------------------------------------------------------------
    // Diálogo de cutscene
    // -------------------------------------------------------------------------

    public void ShowCutsceneDialogue(string text, AudioClip tickClip = null, float tickVolume = 0.3f, int tickEveryN = 2)
    {
        TryReconnectUI();

        if (dialoguePanel == null || dialogueText == null)
        {
            Debug.LogError("[DialogueManager] dialoguePanel ou dialogueText é NULL.");
            return;
        }

        isCutsceneMode = true;
        isActive = true;
        waitingForChoice = false;

        dialoguePanel.SetActive(true);
        StartTypewriter(text, tickClip, tickVolume, tickEveryN);
    }

    public void HideCutsceneDialogue()
    {
        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);

        IsTyping = false;
        isActive = false;
        isCutsceneMode = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (dialogueText != null)
            dialogueText.text = "";
    }

    // -------------------------------------------------------------------------
    // Nodes
    // -------------------------------------------------------------------------

    private void ShowNode(int nodeIndex)
    {
        if (nodes == null || nodeIndex < 0 || nodeIndex >= nodes.Length)
        {
            EndDialogue();
            return;
        }

        HideAllChoiceButtons();
        waitingForChoice = false;
        currentNodeIndex = nodeIndex;

        Interactable.DialogueNode node = nodes[currentNodeIndex];
        StartTypewriter(node.text);

        bool hasChoices = node.choices != null && node.choices.Length > 0;

        if (hasChoices)
        {
            waitingForChoice = true;
            ShowChoices(node.choices);
        }
    }

    private void ShowChoices(Interactable.DialogueChoice[] choices)
    {
        if (choicesContainer == null) return;

        foreach (var choice in choices)
        {
            if (choice.choiceButtonPrefab == null)
            {
                Debug.LogWarning("[DialogueManager] choiceButtonPrefab é NULL numa choice.");
                continue;
            }

            GameObject obj = Instantiate(choice.choiceButtonPrefab, choicesContainer);
            DialogueChoiceButton btn = obj.GetComponent<DialogueChoiceButton>();
            if (btn != null)
                btn.Setup(choice, this);
        }
    }

    private void HideAllChoiceButtons()
    {
        if (choicesContainer == null) return;

        foreach (Transform child in choicesContainer)
            Destroy(child.gameObject);
    }

    // -------------------------------------------------------------------------
    // Avanço e escolhas
    // -------------------------------------------------------------------------

    public void Next()
    {
        if (!isActive || nodes == null || isCutsceneMode) return;
        if (waitingForChoice) return;

        if (IsTyping)
        {
            SkipTypewriter(nodes[currentNodeIndex].text);
            return;
        }

        Interactable.DialogueNode node = nodes[currentNodeIndex];

        if (node.endDialogueHere || node.nextNodeIndex < 0 || node.nextNodeIndex >= nodes.Length)
        {
            EndDialogue();
            return;
        }

        ShowNode(node.nextNodeIndex);
    }

    public void Choose(Interactable.DialogueChoice choice)
    {
        if (!isActive || choice == null) return;

        if (choice.triggerAction)
            Debug.Log(choice.actionDebugMessage);

        choice.onChoiceSelected?.Invoke();

        if (choice.closeDialogue)
        {
            EndDialogue();
            return;
        }

        if (nodes == null || choice.nextNodeIndex < 0 || choice.nextNodeIndex >= nodes.Length)
        {
            EndDialogue();
            return;
        }

        ShowNode(choice.nextNodeIndex);
    }

    // -------------------------------------------------------------------------
    // Fim do diálogo
    // -------------------------------------------------------------------------

    private void EndDialogue()
    {
        Debug.Log($"[DialogueManager] EndDialogue chamado. currentInteractable = {currentInteractable?.name} | ID = {currentInteractable?.interactableId}");

        Interactable endedWith = currentInteractable;
        currentInteractable = null;

        isActive = false;
        waitingForChoice = false;
        isCutsceneMode = false;
        nodes = null;
        currentNodeIndex = 0;

        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);

        IsTyping = false;
        HideAllChoiceButtons();

        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        foreach (var element in uiElementsToHide)
            if (element != null) element.SetActive(true);

        if (crosshair != null) crosshair.SetActive(true);
        if (playerMovementScript != null) playerMovementScript.enabled = true;
        if (playerLookScript != null) playerLookScript.enabled = true;

        if (shouldPauseGame)
            Time.timeScale = 1f;

        shouldPauseGame = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (endedWith != null)
            OnDialogueEnded?.Invoke(endedWith);
    }

    // -------------------------------------------------------------------------
    // Input
    // -------------------------------------------------------------------------

    public bool IsActive() => isActive;

    public void OnNextDialogue(InputAction.CallbackContext context)
    {
        if (!context.performed || !isActive || isCutsceneMode) return;
        if (waitingForChoice) return;
        if (Time.unscaledTime < nextInputAllowedTime) return;

        Next();
    }
}