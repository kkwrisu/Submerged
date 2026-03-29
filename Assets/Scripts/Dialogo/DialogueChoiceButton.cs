using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueChoiceButton : MonoBehaviour
{
    [Header("UI")]
    public Button button;
    public TextMeshProUGUI buttonText;

    private Interactable.DialogueChoice choiceData;
    private DialogueManager dialogueManager;

    public void Setup(Interactable.DialogueChoice choice, DialogueManager manager)
    {
        choiceData = choice;
        dialogueManager = manager;

        if (buttonText != null)
            buttonText.text = choice.choiceText;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (dialogueManager != null && choiceData != null)
        {
            dialogueManager.Choose(choiceData);
        }
    }
}