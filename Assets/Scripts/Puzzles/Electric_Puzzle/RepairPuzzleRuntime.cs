using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RepairPuzzleRuntime : MonoBehaviour
{
    [Header("Scene Difficulty")]
    public RepairPuzzleDifficulty difficulty = RepairPuzzleDifficulty.Difficulty1;

    [Header("References")]
    public Camera puzzleCamera;
    public LineRenderer wireRendererA;
    public LineRenderer wireRendererB;

    [Header("Input Actions")]
    public InputActionReference clickAction;
    public InputActionReference resetAction;

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

    private InputAction click;
    private InputAction reset;

    private void Awake()
    {
        Debug.Log("RepairPuzzleRuntime Awake");
    }

    private void OnEnable()
    {
        Debug.Log("RepairPuzzleRuntime OnEnable");

        click = clickAction != null ? clickAction.action : null;
        reset = resetAction != null ? resetAction.action : null;

        if (click != null)
        {
            click.Enable();
            Debug.Log("Click action enabled: " + click.name);
        }
        else
        {
            Debug.LogError("ClickAction não atribuída no RepairPuzzleRuntime.");
        }

        if (reset != null)
        {
            reset.Enable();
            Debug.Log("Reset action enabled: " + reset.name);
        }
        else
        {
            Debug.LogError("ResetAction não atribuída no RepairPuzzleRuntime.");
        }
    }

    private void OnDisable()
    {
        if (click != null)
            click.Disable();

        if (reset != null)
            reset.Disable();
    }

    private void Start()
    {
        Debug.Log("RepairPuzzleRuntime Start");

        if (puzzleCamera == null)
            puzzleCamera = Camera.main;

        if (puzzleCamera == null)
            Debug.LogError("PuzzleCamera está null.");

        BuildMap();
        FindSpecialNodes();
        ResetPuzzle();
    }

    private void Update()
    {
        if (reset != null && reset.WasPressedThisFrame())
        {
            Debug.Log("RESET PRESSIONADO");
            ResetPuzzle();
            return;
        }

        if (click == null)
            return;

        if (click.WasPressedThisFrame())
        {
            Debug.Log("CLICK PRESSIONADO");
            RepairPuzzleNode node = GetNodeUnderPointer();
            if (node == null)
            {
                Debug.Log("Nenhum node detectado no clique.");
            }
            else
            {
                Debug.Log("Node clicado: " + node.name + " | X=" + node.x + " Y=" + node.y + " | Tipo=" + node.nodeType);
            }

            TryStartDrag(node);
        }

        if (click.IsPressed() && isDragging)
        {
            RepairPuzzleNode node = GetNodeUnderPointer();
            if (node != null)
                TryExtendPath(node);
        }

        if (click.WasReleasedThisFrame())
        {
            Debug.Log("CLICK SOLTO");
            isDragging = false;
        }
    }

    private void BuildMap()
    {
        nodes.Clear();

        RepairPuzzleNode[] allNodes = FindObjectsByType<RepairPuzzleNode>(FindObjectsSortMode.None);

        for (int i = 0; i < allNodes.Length; i++)
        {
            if (allNodes[i].gameObject.scene != gameObject.scene)
                continue;

            Vector2Int pos = new Vector2Int(allNodes[i].x, allNodes[i].y);

            if (!nodes.ContainsKey(pos))
                nodes.Add(pos, allNodes[i]);
            else
                Debug.LogWarning("Node duplicado em: " + pos + " | " + allNodes[i].name);
        }

        Debug.Log("Nodes carregados: " + nodes.Count);
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
                case RepairPuzzleNodeType.StartA: startA = node; break;
                case RepairPuzzleNodeType.EndA: endA = node; break;
                case RepairPuzzleNodeType.StartB: startB = node; break;
                case RepairPuzzleNodeType.EndB: endB = node; break;
            }
        }

        Debug.Log("StartA: " + (startA != null ? startA.name : "NULL"));
        Debug.Log("EndA: " + (endA != null ? endA.name : "NULL"));
        Debug.Log("StartB: " + (startB != null ? startB.name : "NULL"));
        Debug.Log("EndB: " + (endB != null ? endB.name : "NULL"));
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

    private RepairPuzzleNode GetNodeUnderPointer()
    {
        if (puzzleCamera == null)
            return null;

        if (Mouse.current == null)
            return null;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = puzzleCamera.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, Mathf.Abs(puzzleCamera.transform.position.z))
        );

        Collider2D hit = Physics2D.OverlapPoint(new Vector2(worldPos.x, worldPos.y));

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
                {
                    Debug.Log("Clique não foi no StartA.");
                    return;
                }

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
                {
                    Debug.Log("Clique não foi no StartB.");
                    return;
                }

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

        if (difficulty == RepairPuzzleDifficulty.Difficulty4 && activeWire == 2 && pathA.Contains(targetNode))
            return;

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
        Vector2Int[] offsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

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
        Debug.Log("PUZZLE FAIL");
        if (RepairPuzzleManager.Instance != null)
            RepairPuzzleManager.Instance.FinishPuzzle(RepairPuzzleResult.Fail);
    }

    public void SuccessPuzzle()
    {
        Debug.Log("PUZZLE SUCCESS");
        if (RepairPuzzleManager.Instance != null)
            RepairPuzzleManager.Instance.FinishPuzzle(RepairPuzzleResult.Success);
    }
}