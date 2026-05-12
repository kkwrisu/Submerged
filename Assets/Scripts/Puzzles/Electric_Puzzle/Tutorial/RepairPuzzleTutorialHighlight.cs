using System.Collections;
using UnityEngine;

/// <summary>
/// Coloque este componente nos nós da cena que devem ser destacados durante o tutorial.
/// Cria automaticamente um SpriteRenderer filho que simula um glow/outline.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class RepairPuzzleTutorialHighlight : MonoBehaviour
{
    [Header("Glow Config")]
    [Tooltip("Cor do glow ao redor do objeto.")]
    public Color glowColor = new Color(1f, 1f, 0f, 1f);

    [Tooltip("Escala extra do glow em relação ao sprite original.")]
    public float glowScale = 1.25f;

    [Tooltip("Velocidade do pulse de alpha do glow.")]
    public float pulseSpeed = 2f;

    [Tooltip("Alpha mínimo do glow durante o pulse.")]
    [Range(0f, 1f)]
    public float pulseAlphaMin = 0.2f;

    [Tooltip("Alpha máximo do glow durante o pulse.")]
    [Range(0f, 1f)]
    public float pulseAlphaMax = 0.8f;

    // ── Internos ─────────────────────────────────────────────────────────────

    private SpriteRenderer mainRenderer;
    private SpriteRenderer glowRenderer;
    private Coroutine pulseCoroutine;

    // ── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        mainRenderer = GetComponent<SpriteRenderer>();
        CreateGlowRenderer();
        DisableGlow();
    }

    // ── API Pública ──────────────────────────────────────────────────────────

    public void EnableGlow()
    {
        if (glowRenderer == null)
            CreateGlowRenderer();

        glowRenderer.enabled = true;

        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);

        pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    public void DisableGlow()
    {
        if (glowRenderer != null)
            glowRenderer.enabled = false;

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
    }

    // ── Internos ─────────────────────────────────────────────────────────────

    private void CreateGlowRenderer()
    {
        Transform existingGlow = transform.Find("__GlowLayer__");

        if (existingGlow != null)
        {
            glowRenderer = existingGlow.GetComponent<SpriteRenderer>();
            return;
        }

        GameObject glowObj = new GameObject("__GlowLayer__");
        glowObj.transform.SetParent(transform);
        glowObj.transform.localPosition = Vector3.zero;
        glowObj.transform.localRotation = Quaternion.identity;
        glowObj.transform.localScale = Vector3.one * glowScale;

        glowRenderer = glowObj.AddComponent<SpriteRenderer>();
        glowRenderer.sprite = mainRenderer.sprite;
        glowRenderer.sortingLayerID = mainRenderer.sortingLayerID;
        glowRenderer.sortingOrder = mainRenderer.sortingOrder - 1;

        Color c = glowColor;
        c.a = 0f;
        glowRenderer.color = c;
    }

    private IEnumerator PulseRoutine()
    {
        float elapsed = 0f;

        while (true)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = (Mathf.Sin(elapsed * pulseSpeed * Mathf.PI * 2f) + 1f) / 2f;
            float alpha = Mathf.Lerp(pulseAlphaMin, pulseAlphaMax, t);

            Color c = glowColor;
            c.a = alpha;
            glowRenderer.color = c;

            yield return null;
        }
    }
}