using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GeneratorTutorialUI : MonoBehaviour
{
    [Header("Painel principal")]
    public GameObject panelRoot;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Button nextButton;
    public TextMeshProUGUI nextButtonLabel;

    [Header("Overlay escuro (só usado no modo QTE)")]
    public CanvasGroup darkOverlay;
    public float overlayTargetAlpha = 0.75f;
    public float overlayFadeSpeed = 3f;

    [Header("Highlight de UI (modo QTE)")]
    [Tooltip("RectTransform que será movido/escalado para enaltecer o elemento de UI do QTE.")]
    public RectTransform qteHighlightFrame;

    private GeneratorTutorialData data;
    private GeneratorTutorialData.TutorialSlide[] currentSlides;
    private int currentIndex;
    private Action onFinished;
    private Coroutine overlayCoroutine;
    private bool isApproachMode = false;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (darkOverlay != null) { darkOverlay.alpha = 0f; darkOverlay.gameObject.SetActive(false); }
        if (qteHighlightFrame != null) qteHighlightFrame.gameObject.SetActive(false);

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNext);
    }

    // ── Abertura pública ──────────────────────────────────────────────────────

    /// <summary>Tutorial simples de aproximação.</summary>
    public void ShowApproach(GeneratorTutorialData tutorialData, Action callback)
    {
        isApproachMode = true;

        // Libera o cursor para o jogador clicar no botão do tutorial
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Open(tutorialData, tutorialData.approachSlides, callback);
    }

    /// <summary>Tutorial de QTE (com overlay escuro e highlight de UI).</summary>
    public void ShowQTE(GeneratorTutorialData tutorialData, RectTransform qteElementToHighlight, Action callback)
    {
        isApproachMode = false;

        if (qteElementToHighlight != null && qteHighlightFrame != null)
        {
            qteHighlightFrame.position = qteElementToHighlight.position;
            qteHighlightFrame.sizeDelta = qteElementToHighlight.sizeDelta * 1.2f;
            qteHighlightFrame.gameObject.SetActive(true);
        }

        Open(tutorialData, tutorialData.qteSlides, callback);

        if (darkOverlay != null)
        {
            darkOverlay.gameObject.SetActive(true);
            if (overlayCoroutine != null) StopCoroutine(overlayCoroutine);
            overlayCoroutine = StartCoroutine(FadeOverlay(overlayTargetAlpha));
        }
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
        if (titleText != null) titleText.text = slide.title;
        if (descriptionText != null) descriptionText.text = slide.description;

        bool isLast = index == currentSlides.Length - 1;
        if (nextButtonLabel != null)
            nextButtonLabel.text = isLast ? "Entendido!" : "Próximo";
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

        if (qteHighlightFrame != null)
            qteHighlightFrame.gameObject.SetActive(false);

        if (darkOverlay != null)
        {
            if (overlayCoroutine != null) StopCoroutine(overlayCoroutine);
            overlayCoroutine = StartCoroutine(FadeOverlayAndDisable(0f));
        }

        // No modo approach: volta a travar o cursor ao fechar
        if (isApproachMode)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        isApproachMode = false;

        onFinished?.Invoke();
        onFinished = null;
    }

    // ── Overlay ───────────────────────────────────────────────────────────────

    private IEnumerator FadeOverlay(float target)
    {
        while (!Mathf.Approximately(darkOverlay.alpha, target))
        {
            darkOverlay.alpha = Mathf.MoveTowards(
                darkOverlay.alpha, target, overlayFadeSpeed * Time.unscaledDeltaTime);
            yield return null;
        }
        darkOverlay.alpha = target;
    }

    private IEnumerator FadeOverlayAndDisable(float target)
    {
        yield return FadeOverlay(target);
        darkOverlay.gameObject.SetActive(false);
    }
}