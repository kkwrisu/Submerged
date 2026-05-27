using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GeneratorTutorialUI : MonoBehaviour
{
    [Header("Painel principal")]
    public GameObject panelRoot;
    public TextMeshProUGUI descriptionText;
    public Button nextButton;

    [Header("Imagens do botão Next")]
    public ButtonHoverImage nextButtonHover;
    public Sprite spriteProximo;
    public Sprite spriteProximoHover;
    public Sprite spriteEntendido;
    public Sprite spriteEntendidoHover;

    private GeneratorTutorialData data;
    private GeneratorTutorialData.TutorialSlide[] currentSlides;
    private int currentIndex;
    private Action onFinished;
    private bool isApproachMode = false;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNext);
    }

    // ── Abertura pública ──────────────────────────────────────────────────────

    public void ShowApproach(GeneratorTutorialData tutorialData, Action callback)
    {
        isApproachMode = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Open(tutorialData, tutorialData.approachSlides, callback);
    }

    public void ShowQTE(GeneratorTutorialData tutorialData, Action callback)
    {
        isApproachMode = false;
        Open(tutorialData, tutorialData.qteSlides, callback);
    }

    // ── Navegação ─────────────────────────────────────────────────────────────

    private void Open(GeneratorTutorialData tutorialData, GeneratorTutorialData.TutorialSlide[] slides, Action callback)
    {
        data = tutorialData;
        currentSlides = slides;
        currentIndex = 0;
        onFinished = callback;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        ShowSlide(currentIndex);
    }

    private void ShowSlide(int index)
    {
        if (currentSlides == null || index >= currentSlides.Length)
        {
            Close();
            return;
        }

        var slide = currentSlides[index];
        if (descriptionText != null) descriptionText.text = slide.description;

        bool isLast = index == currentSlides.Length - 1;
        if (nextButtonHover != null)
        {
            if (isLast)
                nextButtonHover.SetSprites(spriteEntendido, spriteEntendidoHover);
            else
                nextButtonHover.SetSprites(spriteProximo, spriteProximoHover);
        }
    }

    private void OnNext()
    {
        currentIndex++;
        if (currentIndex < currentSlides.Length)
            ShowSlide(currentIndex);
        else
            Close();
    }

    private void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (isApproachMode)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        isApproachMode = false;
        onFinished?.Invoke();
        onFinished = null;
    }
}