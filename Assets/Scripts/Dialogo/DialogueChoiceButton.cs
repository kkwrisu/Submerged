using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class DialogueChoiceButton : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    public Button button;
    public TextMeshProUGUI buttonText;
    public ButtonHoverImage buttonHoverImage;

    private Interactable.DialogueChoice choiceData;
    private DialogueManager dialogueManager;

    public void Setup(Interactable.DialogueChoice choice, DialogueManager manager)
    {
        choiceData = choice;
        dialogueManager = manager;

        if (buttonHoverImage != null)
            buttonHoverImage.SetSprites(choice.normalSprite, choice.hoverSprite);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);

            if (!string.IsNullOrEmpty(choice.sceneNameToCheck))
                button.interactable = choice.sceneNameToCheck != SceneManager.GetActiveScene().name;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (button == null) return;

        if (button.interactable)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayButtonClick();
        }
        else
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayButtonDisabled();
        }
    }

    private void OnClick()
    {
        if (dialogueManager != null && choiceData != null)
            dialogueManager.Choose(choiceData);
    }
}