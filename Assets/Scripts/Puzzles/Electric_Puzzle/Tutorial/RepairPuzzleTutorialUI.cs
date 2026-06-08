using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI do tutorial do RepairPuzzle — visualmente idêntica ao GeneratorTutorialUI.
/// Navegação apenas para frente (sem botão Prev), sem título.
/// </summary>
public class RepairPuzzleTutorialUI : MonoBehaviour
{
    [Header("Root")]
    public GameObject tutorialRoot;

    [Header("Panel")]
    public GameObject panelRoot;

    [Header("Texto")]
    public TextMeshProUGUI descriptionText;

    [Header("Botão Next — Hover Sprites")]
    public ButtonHoverImage nextButtonHover;
    public Sprite spriteProximo;
    public Sprite spriteProximoHover;
    public Sprite spriteEntendido;
    public Sprite spriteEntendidoHover;

    [Header("Contador de slides (opcional)")]
    public TextMeshProUGUI slideCounterText;

    [Header("Animação")]
    public float fadeDuration = 0.35f;
    public float slideFadeDuration = 0.18f;
    public Image overlayImage;

    private RepairPuzzleTutorialData data;
    private RepairPuzzleRuntime runtime;
    private int currentSlide;
    private Action onFinished;
    private Coroutine fadeCoroutine;
    private List<RepairPuzzleTutorialHighlight> activeHighlights = new();

    // ── API Pública ───────────────────────────────────────────────────────────

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

        Button nextBtn = nextButtonHover != null ? nextButtonHover.GetComponent<Button>() : null;
        if (nextBtn != null)
        {
            nextBtn.onClick.RemoveAllListeners();
            nextBtn.onClick.AddListener(OnNext);
        }

        if (tutorialRoot != null) tutorialRoot.SetActive(true);
        if (panelRoot != null) panelRoot.SetActive(true);

        SetOverlayAlpha(0f);
        SetPanelAlpha(0f);

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

    // ── Navegação ─────────────────────────────────────────────────────────────

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

    private void Finish()
    {
        Hide();
        onFinished?.Invoke();
    }

    // ── Refresh de slide ──────────────────────────────────────────────────────

    private void RefreshSlide()
    {
        if (data == null || currentSlide < 0 || currentSlide >= data.slides.Length) return;

        RepairPuzzleTutorialData.TutorialSlide slide = data.slides[currentSlide];

        if (descriptionText != null)
            descriptionText.text = slide.description;

        if (slideCounterText != null)
            slideCounterText.text = $"{currentSlide + 1} / {data.slides.Length}";

        bool isLast = currentSlide == data.slides.Length - 1;
        if (nextButtonHover != null)
        {
            if (isLast)
                nextButtonHover.SetSprites(spriteEntendido, spriteEntendidoHover);
            else
                nextButtonHover.SetSprites(spriteProximo, spriteProximoHover);
        }

        ClearHighlights();

        if (slide.hasHighlight && runtime != null && slide.highlightEntries != null)
        {
            foreach (var entry in slide.highlightEntries)
                ActivateHighlights(entry.nodeType, entry.highlightColor);
        }
    }

    // ── Transições ────────────────────────────────────────────────────────────

    private IEnumerator TransitionSlide()
    {
        CanvasGroup cg = GetPanelCG();

        if (cg != null)
        {
            float t = 0f, start = cg.alpha;
            while (t < slideFadeDuration)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(start, 0f, t / slideFadeDuration);
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
        const float targetOverlay = 0.82f;
        CanvasGroup cg = GetPanelCG();
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / fadeDuration;
            SetOverlayAlpha(Mathf.Lerp(0f, targetOverlay, p));
            if (cg != null) cg.alpha = Mathf.Lerp(0f, 1f, p);
            yield return null;
        }

        SetOverlayAlpha(targetOverlay);
        if (cg != null) cg.alpha = 1f;
    }

    private IEnumerator FadeOutAndClose()
    {
        float startOverlay = overlayImage != null ? overlayImage.color.a : 0f;
        CanvasGroup cg = GetPanelCG();
        float startPanel = cg != null ? cg.alpha : 1f;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / fadeDuration;
            SetOverlayAlpha(Mathf.Lerp(startOverlay, 0f, p));
            if (cg != null) cg.alpha = Mathf.Lerp(startPanel, 0f, p);
            yield return null;
        }

        SetOverlayAlpha(0f);
        if (panelRoot != null) panelRoot.SetActive(false);
        if (tutorialRoot != null) tutorialRoot.SetActive(false);
    }

    // ── Highlights ────────────────────────────────────────────────────────────

    private void ActivateHighlights(RepairPuzzleNodeType nodeType, Color color)
    {
        if (runtime == null) return;

        foreach (RepairPuzzleNode node in runtime.GetNodesByType(nodeType))
        {
            if (node == null) continue;

            RepairPuzzleTutorialHighlight h = node.GetComponent<RepairPuzzleTutorialHighlight>()
                                            ?? node.gameObject.AddComponent<RepairPuzzleTutorialHighlight>();
            h.EnableGlow(color);
            activeHighlights.Add(h);
        }
    }

    private void ClearHighlights()
    {
        foreach (var h in activeHighlights)
            if (h != null) h.DisableGlow();
        activeHighlights.Clear();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private CanvasGroup GetPanelCG()
    {
        if (panelRoot == null) return null;
        return panelRoot.GetComponent<CanvasGroup>();
    }

    private void SetOverlayAlpha(float a)
    {
        if (overlayImage == null) return;
        Color c = overlayImage.color;
        c.a = a;
        overlayImage.color = c;
    }

    private void SetPanelAlpha(float a)
    {
        CanvasGroup cg = GetPanelCG();
        if (cg != null) cg.alpha = a;
    }
}