using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    [Header("Choices UI")]
    public Transform choicesContainer;
    public GameObject choiceButtonPrefab;

    [Header("Gameplay Lock")]
    public GameObject crosshair;
    public MonoBehaviour playerMovementScript;
    public MonoBehaviour playerLookScript;
    public float inputBlockAfterStart = 0.15f;

    private Interactable.DialogueNode[] nodes;
    private int currentNodeIndex;

    private bool isActive = false;
    private bool waitingForChoice = false;
    private float nextInputAllowedTime = 0f;

    private readonly List<GameObject> spawnedChoices = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        ClearChoices();
    }

    public void StartDialogue(Interactable interactable)
    {
        if (interactable == null)
            return;

        if (interactable.dialogueNodes == null || interactable.dialogueNodes.Length == 0)
            return;

        nodes = interactable.dialogueNodes;
        currentNodeIndex = 0;
        isActive = true;
        waitingForChoice = false;
        nextInputAllowedTime = Time.time + inputBlockAfterStart;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
        else
            return;

        if (crosshair != null)
            crosshair.SetActive(false);

        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        if (playerLookScript != null)
            playerLookScript.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ShowNode(currentNodeIndex);
    }

    private void ShowNode(int nodeIndex)
    {
        if (nodes == null || nodeIndex < 0 || nodeIndex >= nodes.Length)
        {
            EndDialogue();
            return;
        }

        ClearChoices();
        waitingForChoice = false;
        currentNodeIndex = nodeIndex;

        Interactable.DialogueNode node = nodes[currentNodeIndex];

        if (dialogueText != null)
            dialogueText.text = node.text;

        bool hasChoices = node.choices != null && node.choices.Length > 0;

        if (hasChoices)
        {
            waitingForChoice = true;
            SpawnChoices(node.choices);
        }
    }

    private void SpawnChoices(Interactable.DialogueChoice[] choices)
    {
        if (choicesContainer == null || choiceButtonPrefab == null)
            return;

        for (int i = 0; i < choices.Length; i++)
        {
            Interactable.DialogueChoice choiceData = choices[i];
            GameObject choiceObj = Instantiate(choiceButtonPrefab, choicesContainer);
            spawnedChoices.Add(choiceObj);

            DialogueChoiceButton choiceButton = choiceObj.GetComponent<DialogueChoiceButton>();
            if (choiceButton != null)
                choiceButton.Setup(choiceData, this);
        }
    }

    private void ClearChoices()
    {
        for (int i = 0; i < spawnedChoices.Count; i++)
        {
            if (spawnedChoices[i] != null)
                Destroy(spawnedChoices[i]);
        }

        spawnedChoices.Clear();
    }

    public void Next()
    {
        if (!isActive || nodes == null)
            return;

        if (waitingForChoice)
            return;

        Interactable.DialogueNode node = nodes[currentNodeIndex];

        if (node.endDialogueHere)
        {
            EndDialogue();
            return;
        }

        if (node.nextNodeIndex < 0 || node.nextNodeIndex >= nodes.Length)
        {
            EndDialogue();
            return;
        }

        ShowNode(node.nextNodeIndex);
    }

    public void Choose(Interactable.DialogueChoice choice)
    {
        if (!isActive || choice == null)
            return;

        if (choice.triggerAction)
            Debug.Log(choice.actionDebugMessage);

        if (choice.onChoiceSelected != null)
            choice.onChoiceSelected.Invoke();

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

    private void EndDialogue()
    {
        isActive = false;
        waitingForChoice = false;
        nodes = null;
        currentNodeIndex = 0;

        ClearChoices();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (crosshair != null)
            crosshair.SetActive(true);

        if (playerMovementScript != null)
            playerMovementScript.enabled = true;

        if (playerLookScript != null)
            playerLookScript.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public bool IsActive()
    {
        return isActive;
    }

    public void OnNextDialogue(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (!isActive)
            return;

        if (waitingForChoice)
            return;

        if (Time.time < nextInputAllowedTime)
            return;

        Next();
    }
}