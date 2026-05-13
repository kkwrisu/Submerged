using UnityEngine;

/// <summary>
/// Linha em L que conecta visualmente entrada e saída de um par de portais.
/// Fica cinza desde o início; ao chamar Activate() troca para a cor do fio.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class RepairPuzzlePortalGhost : MonoBehaviour
{
    // ── Configuração ──────────────────────────────────────────────────────────

    [Header("Cor idle (antes de conectar)")]
    public Color idleColor = new Color(0.6f, 0.6f, 0.6f, 0.4f);

    [Header("Largura — ajuste até ficar visível na sua cena")]
    public float lineWidth = 0.15f;

    // ── Internos ──────────────────────────────────────────────────────────────

    private LineRenderer lr;
    private bool activated;

    // ── Setup (chamado pelo Runtime logo após AddComponent) ───────────────────

    public void Setup(Vector3[] points, int sortingLayerID, int sortingOrder)
    {
        lr = GetComponent<LineRenderer>();

        // Cria material próprio que garante suporte a vertex color
        lr.material = new Material(Shader.Find("Sprites/Default"));

        lr.useWorldSpace = true;
        lr.loop = false;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.textureMode = LineTextureMode.Stretch;
        lr.sortingLayerID = sortingLayerID;
        lr.sortingOrder = sortingOrder;

        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.startColor = idleColor;
        lr.endColor = idleColor;

        lr.positionCount = points.Length;
        for (int i = 0; i < points.Length; i++)
            lr.SetPosition(i, points[i]);
    }

    // ── Ativação (quando o fio conecta na entrada do portal) ──────────────────

    public void Activate(Color wireColor)
    {
        if (activated) return;
        activated = true;

        // Cor sólida, mesma largura — sem animação
        Color solid = wireColor;
        solid.a = 1f;
        lr.startColor = solid;
        lr.endColor = solid;
    }
}