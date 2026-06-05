using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Substitui o LineRenderer por sprites individuais em cada nó do path.
/// Attach no mesmo GameObject que RepairPuzzleRuntime (ou em qualquer lugar da cena do puzzle).
/// </summary>
public class RepairPuzzleWireTiler : MonoBehaviour
{
    [Header("Sprites")]
    [Tooltip("Sprite de fio reto — deve apontar horizontalmente (→) por padrão.")]
    public Sprite spriteStretch;

    [Tooltip("Sprite de curva — deve curvar de → para ↑ (saindo direita, chegando cima) por padrão.")]
    public Sprite spriteCurve;

    [Header("Visual")]
    public float wireZOffset = -0.1f;
    public int sortingOrder = 5;
    public string sortingLayerName = "Default";

    private readonly List<GameObject> activeTiles = new List<GameObject>();

    // ── API pública ───────────────────────────────────────────────────────────

    public void ClearTiles()
    {
        for (int i = 0; i < activeTiles.Count; i++)
            if (activeTiles[i] != null) Destroy(activeTiles[i]);
        activeTiles.Clear();
    }

    public void BuildTiles(List<RepairPuzzleNode> path, Color wireColor)
    {
        ClearTiles();

        if (path == null || path.Count == 0) return;

        float cellSize = path[0].cellSize; // pega do primeiro nó

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int? fromDir = i > 0
                ? GridDir(path[i - 1], path[i])
                : (Vector2Int?)null;

            Vector2Int? toDir = i < path.Count - 1
                ? GridDir(path[i], path[i + 1])
                : (Vector2Int?)null;

            SpritePiece piece = ResolvePiece(fromDir, toDir);
            SpawnTile(path[i].transform.position, piece, wireColor, cellSize);
        }
    }

    // ── Internos ──────────────────────────────────────────────────────────────

    private enum SpritePiece
    {
        Horizontal,
        Vertical,
        CurveRightUp,
        CurveRightDown,
        CurveLeftUp,
        CurveLeftDown,
    }

    private SpritePiece ResolvePiece(Vector2Int? from, Vector2Int? to)
    {
        // Nó inicial: só tem "to"
        if (from == null && to != null)
            return IsHorizontal(to.Value) ? SpritePiece.Horizontal : SpritePiece.Vertical;

        // Nó final: só tem "from"
        if (from != null && to == null)
            return IsHorizontal(from.Value) ? SpritePiece.Horizontal : SpritePiece.Vertical;

        Vector2Int f = from.Value;
        Vector2Int t = to.Value;

        // Mesmo eixo → reta
        if (f == t || f == -t)
            return IsHorizontal(f) ? SpritePiece.Horizontal : SpritePiece.Vertical;

        // Curva
        Vector2Int arrive = -f; // direção de chegada ao nó
        Vector2Int leave = t;  // direção de saída do nó

        return CurveFor(arrive, leave);
    }

    private SpritePiece CurveFor(Vector2Int arrive, Vector2Int leave)
    {
        if ((arrive == Vector2Int.right && leave == Vector2Int.up) ||
            (arrive == Vector2Int.down && leave == Vector2Int.left))
            return SpritePiece.CurveRightUp;

        if ((arrive == Vector2Int.right && leave == Vector2Int.down) ||
            (arrive == Vector2Int.up && leave == Vector2Int.left))
            return SpritePiece.CurveRightDown;

        if ((arrive == Vector2Int.left && leave == Vector2Int.up) ||
            (arrive == Vector2Int.down && leave == Vector2Int.right))
            return SpritePiece.CurveLeftUp;

        if ((arrive == Vector2Int.left && leave == Vector2Int.down) ||
            (arrive == Vector2Int.up && leave == Vector2Int.right))
            return SpritePiece.CurveLeftDown;

        return SpritePiece.Horizontal;
    }

    private void SpawnTile(Vector3 worldPos, SpritePiece piece, Color color, float cellSize)
    {
        bool isCurve = piece != SpritePiece.Horizontal && piece != SpritePiece.Vertical;
        Sprite sprite = isCurve ? spriteCurve : spriteStretch;

        if (sprite == null) return;

        GameObject go = new GameObject("WireTile");
        go.transform.SetParent(transform);
        go.transform.position = new Vector3(worldPos.x, worldPos.y, worldPos.z + wireZOffset);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = sortingOrder;

        // Escala o sprite para caber exatamente numa célula do grid
        float spriteSize = sprite.bounds.size.x; // assume sprite quadrado
        float scale = cellSize / spriteSize;
        go.transform.localScale = new Vector3(scale, scale, 1f);

        go.transform.rotation = Quaternion.Euler(0f, 0f, GetRotation(piece));
        activeTiles.Add(go);
    }

    private float GetRotation(SpritePiece piece)
    {
        // Convenção do sprite:
        //   spriteStretch  → horizontal (0°)
        //   spriteCurve    → chega pela esquerda (←), sai para cima (↑) = CurveLeftUp a 0°
        //   Ajuste as rotações abaixo se o seu sprite tiver orientação diferente!
        switch (piece)
        {
            case SpritePiece.Horizontal: return 0f;
            case SpritePiece.Vertical: return 90f;
            case SpritePiece.CurveLeftUp: return 0f;
            case SpritePiece.CurveRightUp: return 90f;
            case SpritePiece.CurveRightDown: return 180f;
            case SpritePiece.CurveLeftDown: return 270f;
            default: return 0f;
        }
    }

    private bool IsHorizontal(Vector2Int dir)
        => dir == Vector2Int.right || dir == Vector2Int.left;

    private Vector2Int GridDir(RepairPuzzleNode from, RepairPuzzleNode to)
        => new Vector2Int(
            from.x == to.x ? 0 : (int)Mathf.Sign(to.x - from.x),
            from.y == to.y ? 0 : (int)Mathf.Sign(to.y - from.y)
        );
}