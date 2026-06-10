using System;
using System.Collections;
using UnityEngine;

public enum RepairPuzzleNodeType
{
    Empty,
    Wall,
    RedHazard,
    StartA,
    EndA,
    StartB,
    EndB,
    Portal,
    RedHazardIcon,
    PortalIcon
}

[RequireComponent(typeof(Collider2D))]
public class RepairPuzzleNode : MonoBehaviour
{
    [Header("Grid (auto)")]
    public int x;
    public int y;
    public float cellSize = 1f;

    public RepairPuzzleNodeType nodeType = RepairPuzzleNodeType.Empty;
    public RepairPuzzleNode portalTarget;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public Color normalColor = Color.white;
    public Color pathColor = Color.green;
    public Color blockedColor = Color.gray;

    // ── Hazard reveal ────────────────────────────────────────────────────────
    [Header("Hazard Reveal Animation")]
    [Tooltip("Sprite do ícone de hazard a mostrar sobre o nó ao falhar. " +
             "Se null, usa o próprio spriteRenderer com cor vermelha.")]
    public Sprite hazardRevealSprite;

    [Tooltip("Duração total da animação de reveal (segundos).")]
    public float hazardRevealDuration = 0.45f;

    [Tooltip("Escala máxima durante o pulse (1 = tamanho normal).")]
    public float hazardPulseScale = 1.35f;

    private void Awake()
    {
        UpdateGridPosition();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UpdateGridPosition();
    }
#endif

    public void UpdateGridPosition()
    {
        Vector3 pos = transform.position;
        x = Mathf.RoundToInt(pos.x / cellSize);
        y = Mathf.RoundToInt(pos.y / cellSize);
    }

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public bool IsWalkable()
    {
        return nodeType != RepairPuzzleNodeType.Wall &&
               nodeType != RepairPuzzleNodeType.RedHazard;
    }

    public void SetNormal()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = normalColor;
    }

    public void SetPath()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = pathColor;
    }

    public void SetBlocked()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = blockedColor;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Hazard Reveal
    // Chama a animação de "hazard aparecendo" e invoca onDone ao terminar.
    // ─────────────────────────────────────────────────────────────────────────
    public void PlayHazardReveal(Action onDone = null)
    {
        StartCoroutine(HazardRevealRoutine(onDone));
    }

    private IEnumerator HazardRevealRoutine(Action onDone)
    {
        // Cria um overlay sprite filho para não mexer no sprite original do nó
        GameObject overlay = new GameObject("HazardRevealOverlay");
        overlay.transform.SetParent(transform);
        overlay.transform.localPosition = Vector3.zero;
        overlay.transform.localScale = Vector3.zero;

        SpriteRenderer sr = overlay.AddComponent<SpriteRenderer>();
        sr.sprite = hazardRevealSprite != null ? hazardRevealSprite
                                               : (spriteRenderer != null ? spriteRenderer.sprite : null);

        // Herda sorting do nó pai + 1 para ficar na frente
        if (spriteRenderer != null)
        {
            sr.sortingLayerID = spriteRenderer.sortingLayerID;
            sr.sortingOrder = spriteRenderer.sortingOrder + 1;
        }

        Color baseColor = new Color(1f, 0.15f, 0.15f, 1f);
        sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

        float half = hazardRevealDuration * 0.5f;

        // ── Fase 1: crescer de 0 → pulseScale com fade-in (metade do tempo)
        float t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / half);
            float eased = 1f - Mathf.Pow(1f - p, 3f); // ease-out cubic

            float scaleVal = Mathf.Lerp(0f, hazardPulseScale, eased);
            overlay.transform.localScale = new Vector3(scaleVal, scaleVal, 1f);
            sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, eased);
            yield return null;
        }

        // ── Fase 2: recuar de pulseScale → 1 (segunda metade)
        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / half);
            float eased = p * p; // ease-in quad

            float scaleVal = Mathf.Lerp(hazardPulseScale, 1f, eased);
            overlay.transform.localScale = new Vector3(scaleVal, scaleVal, 1f);
            sr.color = baseColor;
            yield return null;
        }

        // Garante estado final
        overlay.transform.localScale = Vector3.one;
        sr.color = baseColor;

        // Mantém o overlay visível (o Runtime vai destruir a cena inteira logo após)
        onDone?.Invoke();
    }
}