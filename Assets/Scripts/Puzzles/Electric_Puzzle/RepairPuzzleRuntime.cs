using System.Collections.Generic;
using UnityEngine;

public class RepairPuzzleRuntime : MonoBehaviour
{
    [Header("Scene Difficulty")]
    public RepairPuzzleDifficulty difficulty = RepairPuzzleDifficulty.Difficulty1;

    [Header("References")]
    public Camera puzzleCamera;
    public LineRenderer wireRendererA;
    public LineRenderer wireRendererB;

    [Header("Visual")]
    public float wireZOffset = -0.1f;

    private readonly Dictionary<Vector2Int, RepairPuzzleNode> nodes = new Dictionary<Vector2Int, RepairPuzzleNode>();

    private RepairPuzzleNode startA;
    private RepairPuzzleNode endA;
    private RepairPuzzleNode startB;
    private RepairPuzzleNode endB;

    private readonly List<RepairPuzzleNode> pathA = new List<RepairPuzzleNode>();
    private readonly List<RepairPuzzleNode> pathB = new List<RepairPuzzleNode>();

    private bool isDragging;
    private bool wireAComplete;
    private bool wireBComplete;
    private int activeWire = 1;

    private void Start()
    {
        if (puzzleCamera == null)
            puzzleCamera = Camera.main;

        BuildMap();
        FindSpecialNodes();
        ResetPuzzle();
    }

    private void Update()
    {
        HandleInput();
    }

    private void BuildMap()
    {
        nodes.Clear();

        RepairPuzzleNode[] allNodes = FindObjectsByType<RepairPuzzleNode>(FindObjectsSortMode.None); ;
        for (int i = 0; i < allNodes.Length; i++)
        {
            Vector2Int pos = new Vector2Int(allNodes[i].x, allNodes[i].y);

            if (!nodes.ContainsKey(pos))
                nodes.Add(pos, allNodes[i]);
            else
                Debug.LogWarning("Node duplicado em: " + pos);
        }
    }

    private void FindSpecialNodes()
    {
        startA = null;
        endA = null;
        startB = null;
        endB = null;

        foreach (var pair in nodes)
        {
            RepairPuzzleNode node = pair.Value;

            switch (node.nodeType)
            {
                case RepairPuzzleNodeType.StartA:
                    startA = node;
                    break;
                case RepairPuzzleNodeType.EndA:
                    endA = node;
                    break;
                case RepairPuzzleNodeType.StartB:
                    startB = node;
                    break;
                case RepairPuzzleNodeType.EndB:
                    endB = node;
                    break;
            }
        }
    }

    public void ResetPuzzle()
    {
        pathA.Clear();
        pathB.Clear();

        wireAComplete = false;
        wireBComplete = false;
        activeWire = 1;
        isDragging = false;

        RefreshVisuals();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetPuzzle();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            RepairPuzzleNode node = GetNodeUnderMouse();
            TryStartDrag(node);
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            RepairPuzzleNode node = GetNodeUnderMouse();
            if (node != null)
                TryExtendPath(node);
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    private RepairPuzzleNode GetNodeUnderMouse()
    {
        if (puzzleCamera == null)
            return null;

        Vector3 mouseWorld = puzzleCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 point = new Vector2(mouseWorld.x, mouseWorld.y);

        Collider2D hit = Physics2D.OverlapPoint(point);
        if (hit == null)
            return null;

        return hit.GetComponent<RepairPuzzleNode>();
    }

    private void TryStartDrag(RepairPuzzleNode node)
    {
        if (node == null)
            return;

        if (activeWire == 1)
        {
            if (wireAComplete)
                return;

            if (pathA.Count == 0)
            {
                if (node != startA)
                    return;

                pathA.Add(node);
                isDragging = true;
                RefreshVisuals();
                return;
            }

            if (node == pathA[pathA.Count - 1])
                isDragging = true;
        }
        else
        {
            if (wireBComplete)
                return;

            if (pathB.Count == 0)
            {
                if (node != startB)
                    return;

                pathB.Add(node);
                isDragging = true;
                RefreshVisuals();
                return;
            }

            if (node == pathB[pathB.Count - 1])
                isDragging = true;
        }
    }

    private void TryExtendPath(RepairPuzzleNode targetNode)
    {
        List<RepairPuzzleNode> currentPath = activeWire == 1 ? pathA : pathB;
        RepairPuzzleNode currentEnd = currentPath.Count > 0 ? currentPath[currentPath.Count - 1] : null;

        if (currentEnd == null || targetNode == null)
            return;

        if (targetNode == currentEnd)
            return;

        if (!AreOrthogonallyAdjacent(currentEnd, targetNode))
            return;

        if (!targetNode.IsWalkable())
            return;

        if (currentPath.Count >= 2 && targetNode == currentPath[currentPath.Count - 2])
        {
            currentPath.RemoveAt(currentPath.Count - 1);
            RefreshVisuals();
            return;
        }

        if (currentPath.Contains(targetNode))
            return;

        if (targetNode.nodeType == RepairPuzzleNodeType.StartA || targetNode.nodeType == RepairPuzzleNodeType.StartB)
            return;

        if (activeWire == 1 && targetNode.nodeType == RepairPuzzleNodeType.EndB)
            return;

        if (activeWire == 2 && targetNode.nodeType == RepairPuzzleNodeType.EndA)
            return;

        if (difficulty == RepairPuzzleDifficulty.Difficulty4)
        {
            if (activeWire == 2 && pathA.Contains(targetNode))
                return;
        }

        if (IsDangerForStep(targetNode))
        {
            FailPuzzle();
            return;
        }

        currentPath.Add(targetNode);

        if (targetNode.nodeType == RepairPuzzleNodeType.Portal && targetNode.portalTarget != null)
        {
            RepairPuzzleNode exitPortal = targetNode.portalTarget;

            if (difficulty == RepairPuzzleDifficulty.Difficulty4 && activeWire == 2 && pathA.Contains(exitPortal))
            {
                FailPuzzle();
                return;
            }

            if (!currentPath.Contains(exitPortal))
                currentPath.Add(exitPortal);
        }

        RefreshVisuals();
        CheckCompletion();
    }

    private void CheckCompletion()
    {
        if (activeWire == 1)
        {
            if (pathA.Count > 0 && pathA[pathA.Count - 1] == endA)
            {
                wireAComplete = true;
                isDragging = false;

                if (difficulty == RepairPuzzleDifficulty.Difficulty4)
                {
                    activeWire = 2;
                    RefreshVisuals();
                }
                else
                {
                    SuccessPuzzle();
                }
            }
        }
        else
        {
            if (pathB.Count > 0 && pathB[pathB.Count - 1] == endB)
            {
                wireBComplete = true;
                isDragging = false;
                SuccessPuzzle();
            }
        }
    }

    private bool IsDangerForStep(RepairPuzzleNode node)
    {
        if (difficulty == RepairPuzzleDifficulty.Difficulty1)
            return false;

        Vector2Int pos = new Vector2Int(node.x, node.y);

        Vector2Int[] offsets =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector2Int check = pos + offsets[i];
            if (nodes.TryGetValue(check, out RepairPuzzleNode neighbor))
            {
                if (neighbor.nodeType == RepairPuzzleNodeType.RedHazard)
                    return true;
            }
        }

        return false;
    }

    private bool AreOrthogonallyAdjacent(RepairPuzzleNode a, RepairPuzzleNode b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return dx + dy == 1;
    }

    private void RefreshVisuals()
    {
        foreach (var pair in nodes)
            pair.Value.SetNormal();

        for (int i = 0; i < pathA.Count; i++)
            pathA[i].SetPath();

        for (int i = 0; i < pathB.Count; i++)
            pathB[i].SetPath();

        if (startA != null) startA.SetPath();
        if (endA != null) endA.SetPath();

        if (difficulty == RepairPuzzleDifficulty.Difficulty4)
        {
            if (wireAComplete)
            {
                if (startB != null) startB.SetPath();
                if (endB != null) endB.SetPath();
            }
            else
            {
                if (startB != null) startB.SetBlocked();
                if (endB != null) endB.SetBlocked();
            }
        }

        UpdateLineRenderer(wireRendererA, pathA);
        UpdateLineRenderer(wireRendererB, pathB);
    }

    private void UpdateLineRenderer(LineRenderer lr, List<RepairPuzzleNode> path)
    {
        if (lr == null)
            return;

        lr.positionCount = path.Count;

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 pos = path[i].transform.position;
            pos.z += wireZOffset;
            lr.SetPosition(i, pos);
        }
    }

    public void FailPuzzle()
    {
        if (RepairPuzzleManager.Instance != null)
            RepairPuzzleManager.Instance.FinishPuzzle(RepairPuzzleResult.Fail);
    }

    public void SuccessPuzzle()
    {
        if (RepairPuzzleManager.Instance != null)
            RepairPuzzleManager.Instance.FinishPuzzle(RepairPuzzleResult.Success);
    }
}