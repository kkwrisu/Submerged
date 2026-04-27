using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI do minigame de gerador.
///
/// Hierarquia sugerida no Canvas (Screen Space – Overlay):
///
///  GeneratorMinigameUI          ← este componente
///  └── Root                    ← rootPanel (ativado/desativado)
///      ├── ProgressBar
///      │   └── Fill             ← progressFill (Image, type = Filled horizontal)
///      ├── SkillCheck           ← skillCheckRoot (ativado durante QTE)
///      │   ├── Ring             ← Image circular de fundo
///      │   ├── Indicator        ← qteIndicator  (RectTransform que gira)
///      │   ├── SuccessZone      ← qteSuccessZone (arco verde, width proporcional)
///      │   └── HitFlash         ← qteHitFlash (Image que pisca ao acertar/errar)
///      └── CompletionLabel      ← completionLabel (Text/TMP opcional)
/// </summary>
public class GeneratorMinigameUI : MonoBehaviour
{
    // ─── Painel raiz ──────────────────────────────────────────────────────────

    [Header("Root")]
    public GameObject rootPanel;

    // ─── Barra de progresso ───────────────────────────────────────────────────

    [Header("Progress Bar")]
    [Tooltip("Image com Fill Method = Horizontal. FillAmount 0→1.")]
    public Image progressFill;

    public Color progressNormalColor = new Color(0.2f, 0.85f, 0.3f);
    public Color progressDamageColor = new Color(1f, 0.25f, 0.1f);
    public float progressDamageFlashDuration = 0.35f;

    // ─── Skill check (QTE) ───────────────────────────────────────────────────

    [Header("Skill Check")]
    [Tooltip("GameObject pai do skill check (ring + indicator).")]
    public GameObject skillCheckRoot;

    [Tooltip("RectTransform do ponteiro que gira em torno do ring.")]
    public RectTransform qteIndicator;

    [Tooltip("RectTransform da zona verde de sucesso (posicionada no anel).")]
    public RectTransform qteSuccessZone;

    [Tooltip("Image que pisca ao acertar ou errar.")]
    public Image qteHitFlash;

    public Color successFlashColor = new Color(0.2f, 1f, 0.4f, 0.9f);
    public Color failFlashColor = new Color(1f, 0.1f, 0.1f, 0.9f);
    public float flashDuration = 0.25f;

    [Tooltip("Graus que a zona de sucesso ocupa no anel (calculado por qteSuccessWindow / qteTotalWindow * 360).")]
    [Range(5f, 90f)]
    public float successZoneAngleDegrees = 30f;

    // ─── Texto de conclusão ───────────────────────────────────────────────────

    [Header("Completion")]
    public GameObject completionLabel;

    // ─── Estado interno ───────────────────────────────────────────────────────

    private bool progressDamageFlashing;
    private Coroutine progressFlashRoutine;
    private Coroutine hitFlashRoutine;

    // Posição angular aleatória da zona de sucesso (definida ao spawnar o QTE)
    private float successZoneAngle;

    // ─────────────────────────────────────────────────────────────────────────
    //  Show / Hide
    // ─────────────────────────────────────────────────────────────────────────

    public void Show()
    {
        if (rootPanel != null) rootPanel.SetActive(true);
        if (completionLabel != null) completionLabel.SetActive(false);

        HideQTEImmediate();
    }

    public void Hide()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Progresso
    // ─────────────────────────────────────────────────────────────────────────

    public void SetProgress(float t)
    {
        if (progressFill == null) return;

        progressFill.fillAmount = t;

        if (!progressDamageFlashing)
            progressFill.color = progressNormalColor;
    }

    /// <summary>Pisca a barra em vermelho ao errar um QTE.</summary>
    public void FlashProgressDamage()
    {
        if (progressFill == null) return;

        if (progressFlashRoutine != null)
            StopCoroutine(progressFlashRoutine);

        progressFlashRoutine = StartCoroutine(ProgressDamageRoutine());
    }

    private IEnumerator ProgressDamageRoutine()
    {
        progressDamageFlashing = true;
        progressFill.color = progressDamageColor;

        yield return new WaitForSeconds(progressDamageFlashDuration);

        progressFill.color = progressNormalColor;
        progressDamageFlashing = false;
        progressFlashRoutine = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  QTE
    // ─────────────────────────────────────────────────────────────────────────

    public void ShowQTE()
    {
        if (skillCheckRoot != null)
            skillCheckRoot.SetActive(true);

        // Posição aleatória da zona de sucesso no anel
        successZoneAngle = Random.Range(0f, 360f);

        // Posiciona a zona de sucesso
        if (qteSuccessZone != null)
        {
            qteSuccessZone.localEulerAngles = new Vector3(0f, 0f, successZoneAngle);
        }

        // Reseta o ponteiro no início (oposto à zona para dar tempo)
        if (qteIndicator != null)
        {
            // Inicia 180° antes da zona de sucesso para que o jogador veja a aproximação
            float startAngle = successZoneAngle + 180f;
            qteIndicator.localEulerAngles = new Vector3(0f, 0f, startAngle);
        }

        if (qteHitFlash != null)
            qteHitFlash.color = Color.clear;
    }

    /// <summary>
    /// Chamado a cada frame com t = 0→1 (tempo decorrido / janela total).
    /// Gira o ponteiro 360° ao longo do t.
    /// </summary>
    public void SetQTEProgress(float t)
    {
        if (qteIndicator == null) return;

        // Parte de (successZoneAngle + 180) e gira 360° no sentido anti-horário
        float startAngle = successZoneAngle + 180f;
        float currentAngle = startAngle - t * 360f;
        qteIndicator.localEulerAngles = new Vector3(0f, 0f, currentAngle);
    }

    public void HideQTE(bool success)
    {
        if (qteHitFlash != null)
        {
            if (hitFlashRoutine != null)
                StopCoroutine(hitFlashRoutine);

            hitFlashRoutine = StartCoroutine(HitFlashRoutine(success ? successFlashColor : failFlashColor));
        }

        if (!success)
            FlashProgressDamage();

        // Oculta o ring após o flash
        StartCoroutine(HideSkillCheckAfterDelay());
    }

    private IEnumerator HideSkillCheckAfterDelay()
    {
        yield return new WaitForSeconds(flashDuration + 0.05f);
        HideQTEImmediate();
    }

    private void HideQTEImmediate()
    {
        if (skillCheckRoot != null)
            skillCheckRoot.SetActive(false);
    }

    private IEnumerator HitFlashRoutine(Color color)
    {
        if (qteHitFlash == null) yield break;

        qteHitFlash.color = color;
        float timer = 0f;

        while (timer < flashDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(color.a, 0f, timer / flashDuration);
            qteHitFlash.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        qteHitFlash.color = Color.clear;
        hitFlashRoutine = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Conclusão
    // ─────────────────────────────────────────────────────────────────────────

    public void PlayCompletionAnimation()
    {
        HideQTEImmediate();

        if (completionLabel != null)
            completionLabel.SetActive(true);

        if (progressFill != null)
        {
            progressFill.fillAmount = 1f;
            progressFill.color = successFlashColor;
        }

        StartCoroutine(HideAfterDelay(1.5f));
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Hide();
    }
}