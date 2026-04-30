using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla o HUD do minigame de gerador.
///
/// Hierarquia no Canvas (Screen Space - Overlay):
///
///   GeneratorPuzzleUI
///   └── PuzzlePanel
///       ├── ProgressSlider           ← barra horizontal de progresso
///       └── QTEPanel                 ← centro da tela, aparece só no QTE
///           ├── QTEBackground        ← Image círculo escuro de fundo (opcional)
///           ├── QTEKeyIcon           ← Image com ícone do botão (ex: espaço)
///           ├── QTEKeyText           ← TMP_Text "SPACE" centralizado
///           └── QTECircleSlider      ← Slider configurado como radial (ver abaixo)
///               ├── Background       ← transparente ou cinza escuro
///               ├── Fill Area
///               │   └── Fill         ← Image | Image Type: Filled
///               │                       Fill Method: Radial 360
///               │                       Fill Origin: Top
///               │                       Clockwise: desmarcado (esvazia no sentido horário)
///               └── Handle Slide Area
///                   └── Handle       ← DELETE
///
/// CONFIGURAÇÃO DO CÍRCULO:
///   - O QTECircleSlider NÃO usa o componente Slider para o visual circular.
///     Em vez disso, o Fill é uma Image com Fill Method = Radial 360 controlada diretamente.
///   - Remova o componente Slider do QTECircleSlider e deixe só a Image no Fill.
///   - Arraste essa Image diretamente no campo qteCircleFill abaixo.
///
/// TAMANHOS SUGERIDOS:
///   QTEPanel        → Width: 160  Height: 160  (âncora: center-middle)
///   QTEBackground   → Width: 160  Height: 160  | cor #1A1A1A | alfa 200 | sprite círculo
///   QTEKeyIcon      → Width: 80   Height: 80   (âncora: center-middle)
///   QTEKeyText      → "SPACE"  FontSize 18  Bold  cor branca  (âncora: center, Pos Y: -52)
///   Fill (círculo)  → Width: 160  Height: 160  (âncora: center-middle)
///                     cor #FFFFFF → vai para vermelho via código
///                     Fill Method: Radial 360 | Fill Origin: Top | Clockwise: false
/// </summary>
public class GeneratorPuzzleUI : MonoBehaviour
{
    [Header("Painel raiz")]
    public GameObject puzzlePanel;

    [Header("Barra de Progresso")]
    public Slider progressSlider;

    [Header("QTE")]
    public GameObject qtePanel;
    [Tooltip("A Image do fill do círculo (Fill Method = Radial 360)")]
    public Image qteCircleFill;
    public TMP_Text qteKeyText;

    [Header("Textos")]
    public string qtePromptMessage = "SPACE";

    [Header("Animação")]
    public float flashSpeed = 6f;

    private Coroutine _flashCoroutine;

    // ── Unity ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
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

        if (qteKeyText != null)
            qteKeyText.text = qtePromptMessage;

        // Círculo começa cheio (fillAmount = 1) e esvazia conforme o tempo passa
        if (qteCircleFill != null)
        {
            qteCircleFill.fillAmount = 1f;
            qteCircleFill.color = Color.white;
        }

        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);

        _flashCoroutine = StartCoroutine(FlashKeyText());
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

        if (qteKeyText != null)
            qteKeyText.color = Color.white;
    }

    /// <summary>
    /// Atualiza o círculo do QTE (normalized: 1 = cheio/tempo sobrando, 0 = vazio/falhou).
    /// </summary>
    public void UpdateQteTimer(float normalized)
    {
        if (qteCircleFill == null) return;

        qteCircleFill.fillAmount = Mathf.Clamp01(normalized);

        // Branco → amarelo → vermelho conforme urgência
        if (normalized > 0.5f)
            qteCircleFill.color = Color.Lerp(Color.yellow, Color.white, (normalized - 0.5f) * 2f);
        else
            qteCircleFill.color = Color.Lerp(Color.red, Color.yellow, normalized * 2f);
    }

    // ── Coroutines ─────────────────────────────────────────────────────────────

    private IEnumerator FlashKeyText()
    {
        if (qteKeyText == null) yield break;

        float t = 0f;

        while (qtePanel != null && qtePanel.activeSelf)
        {
            t += Time.deltaTime * flashSpeed;
            float alpha = Mathf.Abs(Mathf.Sin(t));
            qteKeyText.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }
    }
}