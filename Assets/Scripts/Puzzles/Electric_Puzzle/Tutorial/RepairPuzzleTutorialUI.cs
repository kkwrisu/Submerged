using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RepairPuzzleTutorialUI : MonoBehaviour
{
    [Header("Root")]
    public GameObject tutorialRoot;

    [Header("Overlay")]
    public Image overlayImage;

    [Header("Panel")]
    public RectTransform panel;

    [Header("Text")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI slideCounterText;

    [Header("Buttons")]
    public Button prevButton;
    public Button nextButton;
    public Button skipButton;
    public TextMeshProUGUI nextButtonLabel;

    [Header("Animation")]
    public float fadeDuration = 0.35f;
    public float slideFadeDuration = 0.18f;

    private RepairPuzzleTutorialData data;
    private RepairPuzzleRuntime runtime;
    private int currentSlide;
    private Action onFinished;
    private Coroutine fadeCoroutine;
    private List<RepairPuzzleTutorialHighlight> activeHighlights = new List<RepairPuzzleTutorialHighlight>();

    public void Show(RepairPuzzleTutorialData tutorialData, RepairPuzzleRuntime puzzleRuntime, Action onFinishedCallback)
    {
        if (tutorialData == null || tutorialData.slides == null || tutorialData.slides.Length == 0)
        {
            onFinishedCallback?.Invoke();
            return;
        }

        data = tutorialData;
        runtime = puzzleRuntime;
        onFinished = onFinishedCallback;
        currentSlide = 0;

        prevButton.onClick.RemoveAllListeners();
        nextButton.onClick.RemoveAllListeners();
        skipButton.onClick.RemoveAllListeners();

        prevButton.onClick.AddListener(OnPrev);
        nextButton.onClick.AddListener(OnNext);
        skipButton.onClick.AddListener(OnSkip);

        tutorialRoot.SetActive(true);

        if (overlayImage != null)
        {
            Color c = overlayImage.color;
            c.a = 0f;
            overlayImage.color = c;
        }

        if (panel != null)
        {
            CanvasGroup cg = panel.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0f;
        }

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeIn());

        RefreshSlide();
    }

    public void Hide()
    {
        ClearHighlights();

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOutAndClose());
    }

    // ── Navegação ────────────────────────────────────────────────────────────

    private void OnNext()
    {
        if (data == null) return;

        if (currentSlide >= data.slides.Length - 1)
        {
            Finish();
            return;
        }

        currentSlide++;
        StartCoroutine(TransitionSlide());
    }

    private void OnPrev()
    {
        if (currentSlide <= 0) return;

        currentSlide--;
        StartCoroutine(TransitionSlide());
    }

    private void OnSkip()
    {
        Finish();
    }

    private void Finish()
    {
        Hide();
        onFinished?.Invoke();
    }

    // ── Visuais ──────────────────────────────────────────────────────────────

    private void RefreshSlide()
    {
        if (data == null || currentSlide < 0 || currentSlide >= data.slides.Length)
            return;

        RepairPuzzleTutorialData.TutorialSlide slide = data.slides[currentSlide];

        if (titleText != null)
            titleText.text = slide.title;

        if (descriptionText != null)
            descriptionText.text = slide.description;

        if (slideCounterText != null)
            slideCounterText.text = (currentSlide + 1) + " / " + data.slides.Length;

        bool isLast = currentSlide == data.slides.Length - 1;
        if (nextButtonLabel != null)
            nextButtonLabel.text = isLast ? "Entendido!" : "Próximo →";

        if (prevButton != null)
            prevButton.interactable = currentSlide > 0;

        ClearHighlights();

        if (slide.hasHighlight && runtime != null && slide.highlightEntries != null)
        {
            for (int i = 0; i < slide.highlightEntries.Length; i++)
                ActivateHighlights(slide.highlightEntries[i].nodeType, slide.highlightEntries[i].highlightColor);
        }
    }

    private IEnumerator TransitionSlide()
    {
        CanvasGroup cg = panel != null ? panel.GetComponent<CanvasGroup>() : null;

        if (cg != null)
        {
            float t = 0f;
            float startAlpha = cg.alpha;

            while (t < slideFadeDuration)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, 0f, t / slideFadeDuration);
                yield return null;
            }

            cg.alpha = 0f;
        }

        RefreshSlide();

        if (cg != null)
        {
            float t = 0f;

            while (t < slideFadeDuration)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, t / slideFadeDuration);
                yield return null;
            }

            cg.alpha = 1f;
        }
    }

    private IEnumerator FadeIn()
    {
        float t = 0f;
        float targetOverlayAlpha = 0.82f;
        CanvasGroup panelCG = panel != null ? panel.GetComponent<CanvasGroup>() : null;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float progress = t / fadeDuration;

            if (overlayImage != null)
            {
                Color c = overlayImage.color;
                c.a = Mathf.Lerp(0f, targetOverlayAlpha, progress);
                overlayImage.color = c;
            }

            if (panelCG != null)
                panelCG.alpha = Mathf.Lerp(0f, 1f, progress);

            yield return null;
        }

        if (overlayImage != null)
        {
            Color c = overlayImage.color;
            c.a = targetOverlayAlpha;
            overlayImage.color = c;
        }

        if (panelCG != null)
            panelCG.alpha = 1f;
    }

    private IEnumerator FadeOutAndClose()
    {
        float t = 0f;
        float startOverlayAlpha = overlayImage != null ? overlayImage.color.a : 0f;
        CanvasGroup panelCG = panel != null ? panel.GetComponent<CanvasGroup>() : null;
        float startPanelAlpha = panelCG != null ? panelCG.alpha : 1f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float progress = t / fadeDuration;

            if (overlayImage != null)
            {
                Color c = overlayImage.color;
                c.a = Mathf.Lerp(startOverlayAlpha, 0f, progress);
                overlayImage.color = c;
            }

            if (panelCG != null)
                panelCG.alpha = Mathf.Lerp(startPanelAlpha, 0f, progress);

            yield return null;
        }

        tutorialRoot.SetActive(false);
    }

    // ── Highlights ───────────────────────────────────────────────────────────

    private void ActivateHighlights(RepairPuzzleNodeType nodeType, Color color)
    {
        if (runtime == null) return;

        List<RepairPuzzleNode> matchingNodes = runtime.GetNodesByType(nodeType);

        for (int i = 0; i < matchingNodes.Count; i++)
        {
            RepairPuzzleNode node = matchingNodes[i];
            if (node == null) continue;

            RepairPuzzleTutorialHighlight highlight = node.GetComponent<RepairPuzzleTutorialHighlight>();
            if (highlight == null)
                highlight = node.gameObject.AddComponent<RepairPuzzleTutorialHighlight>();

            highlight.EnableGlow(color);
            activeHighlights.Add(highlight);
        }
    }

    private void ClearHighlights()
    {
        for (int i = 0; i < activeHighlights.Count; i++)
        {
            if (activeHighlights[i] != null)
                activeHighlights[i].DisableGlow();
        }

        activeHighlights.Clear();
    }
}