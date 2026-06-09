using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RepairPuzzleTutorialHighlight : MonoBehaviour
{
    [Header("Glow Config")]
    public Color glowColor = new Color(1f, 1f, 0f, 1f);
    public float pulseSpeed = 0.6f;
    [Range(0f, 1f)] public float pulseAlphaMin = 0.35f;
    [Range(0f, 1f)] public float pulseAlphaMax = 0.65f;

    private SpriteRenderer mainRenderer;
    private SpriteRenderer glowRenderer;
    private Coroutine pulseCoroutine;

    private int originalMainOrder;
    private int originalGlowOrder;

    private void Awake()
    {
        mainRenderer = GetComponent<SpriteRenderer>();
        CreateGlowRenderer();
        DisableGlow();
    }

    public void EnableGlow(Color color)
    {
        glowColor = color;

        if (glowRenderer == null)
            CreateGlowRenderer();

        // Guarda sorting orders originais
        originalMainOrder = mainRenderer.sortingOrder;
        originalGlowOrder = glowRenderer.sortingOrder;

        // Coloca à frente do backgroundDimmer do Canvas
        mainRenderer.sortingOrder = 999;
        glowRenderer.sortingOrder = 1000;

        glowRenderer.enabled = true;

        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    public void DisableGlow()
    {
        if (glowRenderer != null)
            glowRenderer.enabled = false;

        // Restaura sorting orders originais
        if (mainRenderer != null)
            mainRenderer.sortingOrder = originalMainOrder;

        if (glowRenderer != null)
            glowRenderer.sortingOrder = originalGlowOrder;

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
    }

    private void CreateGlowRenderer()
    {
        Transform existing = transform.Find("__GlowLayer__");
        if (existing != null)
        {
            glowRenderer = existing.GetComponent<SpriteRenderer>();
            return;
        }

        GameObject glowObj = new GameObject("__GlowLayer__");
        glowObj.transform.SetParent(transform);
        glowObj.transform.localPosition = Vector3.zero;
        glowObj.transform.localRotation = Quaternion.identity;
        glowObj.transform.localScale = Vector3.one;

        glowRenderer = glowObj.AddComponent<SpriteRenderer>();
        glowRenderer.sprite = mainRenderer.sprite;
        glowRenderer.sortingLayerID = mainRenderer.sortingLayerID;
        glowRenderer.sortingOrder = mainRenderer.sortingOrder + 1;

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