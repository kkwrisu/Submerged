using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gerencia o overlay visual do tutorial dentro da cena do puzzle.
///
/// SETUP NA CENA DO PUZZLE:
/// Crie uma Canvas (Screen Space - Overlay, Sort Order alto como 100) com a hierarquia:
///
///   Canvas
///   └── TutorialRoot (este componente)
///       ├── Overlay (Image, cor preta, alpha ~0.82, raycastTarget ON) ← fundo escuro
///       ├── Panel (RectTransform centralizado, ~720x460)
///       │   ├── ElementHighlight (Image) ← sprite do elemento
///       │   ├── GlowRing (Image, sprite circular com blur) ← anel de glow ao redor
///       │   ├── TitleText (TextMeshProUGUI)
///       │   ├── DescriptionText (TextMeshProUGUI)
///       │   ├── ControlsPanel (GameObject com filhos de ícones — oculto por padrão)
///       │   │   ├── MouseIcon (Image)
///       │   │   └── ControlsDescription (TextMeshProUGUI)
///       │   ├── SlideCounter (TextMeshProUGUI) ← "1 / 5"
///       │   ├── PrevButton (Button)
///       │   └── NextButton (Button) ← último slide vira "Entendido!"
///       └── SkipButton (Button, canto superior direito)
/// </summary>
public class RepairPuzzleTutorialUI : MonoBehaviour
{
    // ── Referências ─────────────────────────────────────────────────────────

    [Header("Root")]
    [Tooltip("GameObject raiz do tutorial. Ativado/desativado conforme o estado.")]
    public GameObject tutorialRoot;

    [Header("Overlay")]
    [Tooltip("Imagem preta semitransparente que cobre o jogo.")]
    public Image overlayImage;

    [Header("Panel")]
    public RectTransform panel;

    [Header("Element Display")]
    [Tooltip("Imagem que exibe o sprite do elemento sendo explicado.")]
    public Image elementHighlight;

    [Tooltip("Anel de glow ao redor do sprite (opcional).")]
    public Image glowRing;

    [Header("Text")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI slideCounterText;

    [Header("Controls Slide")]
    [Tooltip("Painel alternativo exibido no slide de controles.")]
    public GameObject controlsPanel;

    [Header("Buttons")]
    public Button prevButton;
    public Button nextButton;
    public Button skipButton;
    public TextMeshProUGUI nextButtonLabel;

    [Header("Animation")]
    [Tooltip("Duração do fade de entrada/saída do overlay.")]
    public float fadeDuration = 0.35f;

    [Tooltip("Duração da transição entre slides.")]
    public float slideFadeDuration = 0.18f;

    // ── Estado interno ───────────────────────────────────────────────────────

    private RepairPuzzleTutorialData data;
    private int currentSlide;
    private Action onFinished;
    private Coroutine fadeCoroutine;

    // ── API Pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Abre o tutorial com os dados fornecidos.
    /// <paramref name="onFinishedCallback"/> é chamado quando o jogador termina ou pula.
    /// </summary>
    public void Show(RepairPuzzleTutorialData tutorialData, Action onFinishedCallback)
    {
        if (tutorialData == null || tutorialData.slides == null || tutorialData.slides.Length == 0)
        {
            onFinishedCallback?.Invoke();
            return;
        }

        data = tutorialData;
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

        RefreshSlide(false);
    }

    public void Hide()
    {
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

    private void RefreshSlide(bool animate)
    {
        if (data == null || currentSlide < 0 || currentSlide >= data.slides.Length)
            return;

        RepairPuzzleTutorialData.TutorialSlide slide = data.slides[currentSlide];

        // Textos
        if (titleText != null)
            titleText.text = slide.title;

        if (descriptionText != null)
            descriptionText.text = slide.description;

        if (slideCounterText != null)
            slideCounterText.text = (currentSlide + 1) + " / " + data.slides.Length;

        // Botão next / Entendido
        bool isLast = currentSlide == data.slides.Length - 1;
        if (nextButtonLabel != null)
            nextButtonLabel.text = isLast ? "Entendido!" : "Próximo →";

        // Botão prev
        if (prevButton != null)
            prevButton.interactable = currentSlide > 0;

        // Sprite do elemento
        bool showControls = slide.isControlsSlide;

        if (controlsPanel != null)
            controlsPanel.SetActive(showControls);

        if (elementHighlight != null)
        {
            elementHighlight.gameObject.SetActive(!showControls && slide.elementSprite != null);

            if (!showControls && slide.elementSprite != null)
            {
                elementHighlight.sprite = slide.elementSprite;
                elementHighlight.color = slide.highlightColor;
            }
        }

        if (glowRing != null)
        {
            glowRing.gameObject.SetActive(!showControls && slide.elementSprite != null);

            if (!showControls && slide.elementSprite != null)
            {
                Color gc = slide.highlightColor;
                gc.a = 0.35f;
                glowRing.color = gc;
            }
        }

        // Anima o ícone (pulse)
        if (!showControls && elementHighlight != null && elementHighlight.gameObject.activeSelf)
        {
            StopAllCoroutines(); // só pare coroutines de animação; cuidado se usar outras
            StartCoroutine(FadeIn()); // re-agenda o fade caso venha de transição
            StartCoroutine(PulseIcon());
        }
    }

    private IEnumerator TransitionSlide()
    {
        // Fade out do conteúdo do painel
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

        RefreshSlide(true);

        // Fade in do conteúdo
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

    private IEnumerator PulseIcon()
    {
        if (elementHighlight == null) yield break;

        RectTransform rt = elementHighlight.GetComponent<RectTransform>();
        if (rt == null) yield break;

        Vector3 baseScale = Vector3.one;
        float pulseSpeed = 1.4f;
        float pulseAmount = 0.07f;
        float elapsed = 0f;

        while (elementHighlight.gameObject.activeSelf)
        {
            elapsed += Time.unscaledDeltaTime;
            float scale = 1f + Mathf.Sin(elapsed * pulseSpeed * Mathf.PI * 2f) * pulseAmount;
            rt.localScale = baseScale * scale;
            yield return null;
        }

        rt.localScale = baseScale;
    }
}