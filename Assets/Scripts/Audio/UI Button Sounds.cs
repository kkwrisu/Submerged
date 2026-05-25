using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        AudioManager.Instance?.PlayButtonHover();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_button.interactable)
            AudioManager.Instance?.PlayButtonClick();
        else
            AudioManager.Instance?.PlayButtonDisabled();
    }
}