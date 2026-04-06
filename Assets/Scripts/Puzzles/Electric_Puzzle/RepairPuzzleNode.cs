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
    public int x;
    public int y;
    public RepairPuzzleNodeType nodeType = RepairPuzzleNodeType.Empty;
    public RepairPuzzleNode portalTarget;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public Color normalColor = Color.white;
    public Color pathColor = Color.green;
    public Color blockedColor = Color.gray;

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public bool IsWalkable()
    {
        return nodeType != RepairPuzzleNodeType.Wall;
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