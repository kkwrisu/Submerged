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
    Portal
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
        if (spriteRenderer == null)
            return;

        spriteRenderer.color = normalColor;
    }

    public void SetPath()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.color = pathColor;
    }

    public void SetBlocked()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.color = blockedColor;
    }
}