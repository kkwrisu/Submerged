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

    private Interactable.DialogueNode[] nodes;
    private int currentNodeIndex;

    private bool isActive = false;
    private bool waitingForChoice = false;

    private readonly List<GameObject> spawnedChoices = new List<GameObject>();

    private void Awake()
    {
        Debug.Log("DialogueManager Awake chamado.");

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        Debug.Log("DialogueManager Start chamado.");

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        else
            Debug.LogError("dialoguePanel não foi atribuído no Inspector.");

        ClearChoices();
    }

    public void StartDialogue(Interactable interactable)
    {
        Debug.Log("StartDialogue chamado.");

        if (interactable == null)
        {
            Debug.LogError("Interactable veio nulo.");
            return;
        }

        if (interactable.dialogueNodes == null || interactable.dialogueNodes.Length == 0)
        {
            Debug.LogError("dialogueNodes vazio.");
            return;
        }

        nodes = interactable.dialogueNodes;
        currentNodeIndex = 0;
        isActive = true;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            Debug.Log("DialoguePanel ativado.");
        }
        else
        {
            Debug.LogError("dialoguePanel está NULL no DialogueManager.");
            return;
        }

        ShowNode(currentNodeIndex);
    }

    private void ShowNode(int nodeIndex)
    {
        if (nodes == null || nodeIndex < 0 || nodeIndex >= nodes.Length)
        {
            Debug.LogError("Índice de nó inválido.");
            EndDialogue();
            return;
        }

        ClearChoices();
        waitingForChoice = false;
        currentNodeIndex = nodeIndex;

        Interactable.DialogueNode node = nodes[currentNodeIndex];

        if (dialogueText != null)
        {
            dialogueText.text = node.text;
            Debug.Log("Texto exibido: " + node.text);
        }
        else
        {
            Debug.LogError("dialogueText está NULL no DialogueManager.");
        }

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
        {
            Debug.LogWarning("choicesContainer ou choiceButtonPrefab não configurado.");
            return;
        }

        for (int i = 0; i < choices.Length; i++)
        {
            Interactable.DialogueChoice choiceData = choices[i];
            GameObject choiceObj = Instantiate(choiceButtonPrefab, choicesContainer);
            spawnedChoices.Add(choiceObj);

            DialogueChoiceButton choiceButton = choiceObj.GetComponent<DialogueChoiceButton>();
            if (choiceButton != null)
            {
                choiceButton.Setup(choiceData, this);
            }
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

        Next();
    }
}