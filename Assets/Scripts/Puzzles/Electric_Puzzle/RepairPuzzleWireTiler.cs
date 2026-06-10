using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairPuzzleWireTiler : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite spriteStretch;
    public Sprite spriteCurveDown;
    public Sprite spriteCurveUp;

    [Header("Visual")]
    public float wireZOffset = -0.1f;
    public int sortingOrder = 5;
    public string sortingLayerName = "Default";

    [Header("Wire Break Animation")]
    [Tooltip("Duração do fade-out + deslocamento ao quebrar o fio.")]
    public float breakDuration = 0.4f;

    [Tooltip("Distância máxima de deslocamento aleatório de cada tile ao quebrar.")]
    public float breakScatter = 0.18f;

    [Tooltip("Cor flash do fio no instante da quebra (antes do fade).")]
    public Color breakFlashColor = new Color(1f, 0.3f, 0.1f, 1f);

    private readonly Dictionary<int, List<GameObject>> segmentTiles =
        new Dictionary<int, List<GameObject>>();

    public void RebuildSegment(int segmentIndex, List<RepairPuzzleNode> path,
                               float forcedCellSize = 0f, bool skipFirstNode = false, bool skipLastNode = false)
    {
        ClearSegment(segmentIndex);

        if (path == null || path.Count == 0) return;

        float cellSize = forcedCellSize > 0f
            ? forcedCellSize
            : path.Count >= 2
                ? Vector3.Distance(path[0].transform.position, path[1].transform.position)
                : path[0].cellSize;

        List<GameObject> bucket = new List<GameObject>();

        for (int i = 0; i < path.Count; i++)
        {
            if (i == 0 && skipFirstNode) continue;
            if (i == path.Count - 1 && skipLastNode) continue;

            Vector2Int? fromDir = i > 0 ? GridDir(path[i - 1], path[i]) : (Vector2Int?)null;
            Vector2Int? toDir = i < path.Count - 1 ? GridDir(path[i], path[i + 1]) : (Vector2Int?)null;

            ResolvePiece(fromDir, toDir, out Sprite sprite, out float rotation);
            GameObject tile = SpawnTile(path[i].transform.position, sprite, rotation, cellSize);
            if (tile != null) bucket.Add(tile);
        }

        segmentTiles[segmentIndex] = bucket;
    }

    public void ClearAll()
    {
        foreach (var kv in segmentTiles)
            DestroyTileList(kv.Value);

        segmentTiles.Clear();
    }

    public void PlayBreakAnimation(int segmentIndex, Action onDone = null)
    {
        if (!segmentTiles.TryGetValue(segmentIndex, out List<GameObject> tiles) || tiles == null || tiles.Count == 0)
        {
            onDone?.Invoke();
            return;
        }

        segmentTiles.Remove(segmentIndex);

        StartCoroutine(BreakRoutine(tiles, onDone));
    }

    public void PlayBreakAnimationAll(Action onDone = null)
    {
        List<int> keys = new List<int>(segmentTiles.Keys);

        if (keys.Count == 0)
        {
            onDone?.Invoke();
            return;
        }

        int remaining = keys.Count;
        Action onEach = () =>
        {
            remaining--;
            if (remaining <= 0)
                onDone?.Invoke();
        };

        for (int i = 0; i < keys.Count; i++)
            PlayBreakAnimation(keys[i], onEach);
    }

    private IEnumerator BreakRoutine(List<GameObject> tiles, Action onDone)
    {
        Vector3[] offsets = new Vector3[tiles.Count];
        SpriteRenderer[] srs = new SpriteRenderer[tiles.Count];
        Color[] startColors = new Color[tiles.Count];

        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i] == null) continue;

            srs[i] = tiles[i].GetComponent<SpriteRenderer>();

            if (srs[i] != null)
            {
                srs[i].color = breakFlashColor;
                startColors[i] = breakFlashColor;
            }

            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = UnityEngine.Random.Range(breakScatter * 0.4f, breakScatter);
            offsets[i] = new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0f);
        }

        yield return null;

        float elapsed = 0f;
        while (elapsed < breakDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / breakDuration);
            float eased = p * p;

            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] == null) continue;

                tiles[i].transform.position = tiles[i].transform.position
                    + offsets[i] * Time.unscaledDeltaTime / breakDuration;

                if (srs[i] != null)
                {
                    Color c = startColors[i];
                    c.a = Mathf.Lerp(1f, 0f, eased);
                    srs[i].color = c;
                }
            }

            yield return null;
        }

        for (int i = 0; i < tiles.Count; i++)
            if (tiles[i] != null) Destroy(tiles[i]);

        onDone?.Invoke();
    }

    private void ClearSegment(int segmentIndex)
    {
        if (segmentTiles.TryGetValue(segmentIndex, out List<GameObject> old))
        {
            DestroyTileList(old);
            segmentTiles.Remove(segmentIndex);
        }
    }

    private void DestroyTileList(List<GameObject> list)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i] != null) Destroy(list[i]);
    }

    private void ResolvePiece(Vector2Int? from, Vector2Int? to, out Sprite sprite, out float rotation)
    {
        if (from == null && to == null)
        {
            sprite = spriteStretch; rotation = 0f; return;
        }

        if (from == null || to == null)
        {
            Vector2Int dir = (from ?? to).Value;
            sprite = spriteStretch;
            rotation = IsHorizontal(dir) ? 0f : 90f;
            return;
        }

        Vector2Int f = from.Value;
        Vector2Int t = to.Value;

        if (f == t || f == -t)
        {
            sprite = spriteStretch;
            rotation = IsHorizontal(f) ? 0f : 90f;
            return;
        }

        ResolveCurve(-f, t, out sprite, out rotation);
    }

    private void ResolveCurve(Vector2Int arrive, Vector2Int leave, out Sprite sprite, out float rotation)
    {
        if (arrive == Vector2Int.left && leave == Vector2Int.down) { sprite = spriteCurveDown; rotation = 0f; return; }
        if (arrive == Vector2Int.down && leave == Vector2Int.right) { sprite = spriteCurveDown; rotation = 90f; return; }
        if (arrive == Vector2Int.right && leave == Vector2Int.up) { sprite = spriteCurveDown; rotation = 180f; return; }
        if (arrive == Vector2Int.up && leave == Vector2Int.left) { sprite = spriteCurveDown; rotation = 270f; return; }
        if (arrive == Vector2Int.left && leave == Vector2Int.up) { sprite = spriteCurveUp; rotation = 0f; return; }
        if (arrive == Vector2Int.down && leave == Vector2Int.left) { sprite = spriteCurveUp; rotation = 90f; return; }
        if (arrive == Vector2Int.right && leave == Vector2Int.down) { sprite = spriteCurveUp; rotation = 180f; return; }
        if (arrive == Vector2Int.up && leave == Vector2Int.right) { sprite = spriteCurveUp; rotation = 270f; return; }

        sprite = spriteStretch; rotation = 0f;
    }

    private GameObject SpawnTile(Vector3 worldPos, Sprite sprite, float rotation, float cellSize)
    {
        if (sprite == null) return null;

        GameObject go = new GameObject("WireTile");
        go.transform.SetParent(transform);
        go.transform.position = new Vector3(worldPos.x, worldPos.y, worldPos.z + wireZOffset);
        go.transform.rotation = Quaternion.Euler(0f, 0f, rotation);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.white;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = sortingOrder;

        float spriteLocalSize = sprite.rect.width / sprite.pixelsPerUnit;
        float parentLossyScale = transform.lossyScale.x;
        float localScale = (spriteLocalSize > 0f && parentLossyScale > 0f)
            ? cellSize / (spriteLocalSize * parentLossyScale)
            : 1f;

        go.transform.localScale = new Vector3(localScale, localScale, 1f);

        return go;
    }

    private bool IsHorizontal(Vector2Int dir)
        => dir == Vector2Int.right || dir == Vector2Int.left;

    private Vector2Int GridDir(RepairPuzzleNode from, RepairPuzzleNode to)
        => new Vector2Int(
            from.x == to.x ? 0 : (int)Mathf.Sign(to.x - from.x),
            from.y == to.y ? 0 : (int)Mathf.Sign(to.y - from.y)
        );
}