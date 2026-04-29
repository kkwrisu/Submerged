using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla o HUD do minigame de gerador.
///
/// Hierarquia sugerida no Canvas (Screen Space - Overlay):
///
///   GeneratorPuzzleUI (este componente)
///   └── PuzzlePanel                      ← raiz que mostra/esconde tudo
///       ├── ProgressSlider               ← Slider (Interactable = false)
///       │   ├── Background               ← Image | #1A1A1A | Source Image = None
///       │   ├── Fill Area
///       │   │   └── Fill                 ← Image | #F5A623 | Source Image = None
///       │   └── Handle Slide Area
///       │       └── Handle               ← DELETE
///       └── QTEPanel                     ← começa desativado
///           ├── QTEPromptText            ← TMP_Text "Pressione [E]!"
///           └── QTETimerSlider           ← Slider (Interactable = false, Value = 1)
///               ├── Background           ← Image | #1A1A1A | Source Image = None
///               ├── Fill Area
///               │   └── Fill             ← Image | #FFD700 | Source Image = None
///               └── Handle Slide Area
///                   └── Handle           ← DELETE
/// </summary>
public class GeneratorPuzzleUI : MonoBehaviour
{
    [Header("Painel raiz")]
    public GameObject puzzlePanel;

    [Header("Barra de Progresso")]
    public Slider progressSlider;

    [Header("QTE")]
    public GameObject qtePanel;
    public TMP_Text qtePromptText;
    public Slider qteTimerSlider;

    [Header("Textos")]
    [Tooltip("Texto exibido no prompt do QTE")]
    public string qtePromptMessage = "Pressione [E]!";

    [Header("Animação")]
    public float flashSpeed = 8f;

    private Coroutine _flashCoroutine;
    private Image _qteTimerFillImage;

    // ── Unity ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Cacheia a Image do fill do QTE timer para trocar cor
        if (qteTimerSlider != null && qteTimerSlider.fillRect != null)
            _qteTimerFillImage = qteTimerSlider.fillRect.GetComponent<Image>();

        Hide();
    }

    // ── API Pública ────────────────────────────────────────────────────────────

    public void Show(float initialProgress)
    {
        if (puzzlePanel != null)
            puzzlePanel.SetActive(true);

        SetProgress(initialProgress);
        HideQTE();
    }

    public void Hide()
    {
        if (puzzlePanel != null)
            puzzlePanel.SetActive(false);

        HideQTE();
    }

    public void SetProgress(float normalized)
    {
        if (progressSlider == null) return;
        progressSlider.value = Mathf.Clamp01(normalized);
    }

    public void ShowQTE(float windowSeconds)
    {
        if (qtePanel != null)
            qtePanel.SetActive(true);

        if (qtePromptText != null)
            qtePromptText.text = qtePromptMessage;

        if (qteTimerSlider != null)
            qteTimerSlider.value = 1f;

        // Restaura cor inicial do timer
        if (_qteTimerFillImage != null)
            _qteTimerFillImage.color = Color.yellow;

        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);

        _flashCoroutine = StartCoroutine(FlashQTE());
    }

    public void HideQTE()
    {
        if (qtePanel != null)
            qtePanel.SetActive(false);

        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
        }

        if (qtePromptText != null)
            qtePromptText.color = Color.white;
    }

    /// <summary>Atualiza o timer visual do QTE (0 a 1, onde 1 = cheio / tempo restante).</summary>
    public void UpdateQteTimer(float normalized)
    {
        if (qteTimerSlider == null) return;

        qteTimerSlider.value = Mathf.Clamp01(normalized);

        // Dourado → vermelho conforme urgência
        if (_qteTimerFillImage != null)
            _qteTimerFillImage.color = Color.Lerp(Color.red, Color.yellow, normalized);
    }

    // ── Coroutines ─────────────────────────────────────────────────────────────

    private IEnumerator FlashQTE()
    {
        if (qtePromptText == null) yield break;

        float t = 0f;

        while (qtePanel != null && qtePanel.activeSelf)
        {
            t += Time.deltaTime * flashSpeed;
            float alpha = Mathf.Abs(Mathf.Sin(t));
            qtePromptText.color = new Color(1f, 1f, 0f, alpha); // amarelo piscando
            yield return null;
        }
    }
}