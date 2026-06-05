using System.Collections;
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

    [Header("Wire Tiler")]
    [Tooltip("Componente responsável por desenhar o fio com sprites. " +
             "Arraste aqui o RepairPuzzleWireTiler do mesmo GameObject.")]
    public RepairPuzzleWireTiler wireTiler;

    [Header("Portal Wire Visual")]
    [Tooltip("Largura do fio gerado após portal.")]
    public float portalWireWidth = 0.1f;
    [Tooltip("Gradiente de cor dos fios pós-portal. Cada par de portal usa uma cor da lista.")]
    public Color[] portalWireColors = new Color[]
    {
        new Color(0.2f, 0.8f, 1f),
        new Color(1f, 0.5f, 0f),
        new Color(0.8f, 0.2f, 1f),
        new Color(0.2f, 1f, 0.5f),
    };

    private readonly Dictionary<Vector2Int, RepairPuzzleNode> nodes = new Dictionary<Vector2Int, RepairPuzzleNode>();

    private RepairPuzzleNode startA;
    private RepairPuzzleNode endA;
    private RepairPuzzleNode startB;
    private RepairPuzzleNode endB;

    private class WireSegment
    {
        public List<RepairPuzzleNode> path = new List<RepairPuzzleNode>();
        public RepairPuzzleNode expectedEnd;
        public LineRenderer lineRenderer;   // mantido apenas para compatibilidade; nunca desenha
        public bool complete;
    }

    private readonly List<WireSegment> segments = new List<WireSegment>();
    private int activeSegmentIndex = 0;
    private bool isDragging;

    private readonly Dictionary<RepairPuzzleNode, RepairPuzzlePortalGhost> portalGhosts
        = new Dictionary<RepairPuzzleNode, RepairPuzzlePortalGhost>();

    private readonly Dictionary<RepairPuzzleNode, int> portalColorIndex
        = new Dictionary<RepairPuzzleNode, int>();

    private Bounds gridBounds;
    private bool lockedByTutorial;

    private InputAction click;
    private InputAction reset;

    // ── Unity lifecycle ────────────────────────────────────────────────────────

    private void Awake()
    {
        Debug.Log("RepairPuzzleRuntime Awake");
    }

    private void OnEnable()
    {
        click = clickAction != null ? clickAction.action : null;
        reset = resetAction != null ? resetAction.action : null;

        if (click != null) click.Enable();
        if (reset != null) reset.Enable();
    }

    private void OnDisable()
    {
        if (click != null) click.Disable();
        if (reset != null) reset.Disable();
    }

    private void Start()
    {
        Debug.Log("RepairPuzzleRuntime Start");

        if (puzzleCamera == null)
            puzzleCamera = FindPuzzleCameraInMyScene();

        if (puzzleCamera == null)
        {
            Debug.LogError("PuzzleCamera está null.");
            return;
        }

        BuildMap();
        FindSpecialNodes();
        ResetPuzzle();

        RepairPuzzleTutorialTracker tracker = GetComponent<RepairPuzzleTutorialTracker>();
        if (tracker != null)
            StartCoroutine(TryShowTutorialNextFrame(tracker));
    }

    // ── Helpers de inicialização ───────────────────────────────────────────────

    private IEnumerator TryShowTutorialNextFrame(RepairPuzzleTutorialTracker tracker)
    {
        yield return null;

        bool tutorialOpened = tracker.TryShowTutorial();

        if (tutorialOpened)
        {
            lockedByTutorial = true;
            Debug.Log("[Runtime] Bloqueado pelo tutorial.");
        }
    }

    public void UnlockAfterTutorial()
    {
        lockedByTutorial = false;
        Debug.Log("[Runtime] Desbloqueado pelo tutorial — jogo começa agora.");
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (RepairPuzzleManager.Instance == null || !RepairPuzzleManager.Instance.IsPuzzleOpen())
            return;

        // ESC: fecha o puzzle sem consequência e bloqueia o PauseMenu por um instante
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            PauseMenu.Instance?.BlockPauseForOneFrame();
            RepairPuzzleManager.Instance.FinishPuzzle(RepairPuzzleResult.None);
            return;
        }

        if (lockedByTutorial)
            return;

        bool resetPressed =
            (reset != null && reset.WasPressedThisFrame()) ||
            (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame);

        if (resetPressed)
        {
            ResetPuzzle();
            return;
        }

        bool clickPressed =
            (click != null && click.WasPressedThisFrame()) ||
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        bool clickHeld =
            (click != null && click.IsPressed()) ||
            (Mouse.current != null && Mouse.current.leftButton.isPressed);

        bool clickReleased =
            (click != null && click.WasReleasedThisFrame()) ||
            (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame);

        if (clickPressed)
        {
            RepairPuzzleNode node = GetNodeUnderPointer();
            TryStartDrag(node);
        }

        if (clickHeld && isDragging)
        {
            RepairPuzzleNode node = GetNodeUnderPointer();
            if (node != null)
                TryExtendPath(node);

            RefreshVisuals();
        }

        if (clickReleased)
        {
            isDragging = false;
            RefreshVisuals();
        }
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    private Camera FindPuzzleCameraInMyScene()
    {
        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

        for (int i = 0; i < allCameras.Length; i++)
        {
            if (allCameras[i].gameObject.scene == gameObject.scene)
                return allCameras[i];
        }

        return null;
    }

    private void BuildMap()
    {
        nodes.Clear();

        RepairPuzzleNode[] allNodes = FindObjectsByType<RepairPuzzleNode>(FindObjectsSortMode.None);

        for (int i = 0; i < allNodes.Length; i++)
        {
            if (allNodes[i].gameObject.scene != gameObject.scene)
                continue;

            allNodes[i].UpdateGridPosition();

            Vector2Int pos = new Vector2Int(allNodes[i].x, allNodes[i].y);

            if (!nodes.ContainsKey(pos))
                nodes.Add(pos, allNodes[i]);
            else
                Debug.LogWarning("Node duplicado em: " + pos);
        }

        Debug.Log("Nodes carregados: " + nodes.Count);

        if (nodes.Count > 0)
        {
            bool first = true;
            foreach (var pair in nodes)
            {
                Vector3 wp = pair.Value.transform.position;
                if (first) { gridBounds = new Bounds(wp, Vector3.zero); first = false; }
                else gridBounds.Encapsulate(wp);
            }
            gridBounds.Expand(GetNodeSpacing());
        }
    }

    private float GetNodeSpacing()
    {
        foreach (var pair in nodes)
        {
            Vector2Int pos = new Vector2Int(pair.Value.x, pair.Value.y);
            Vector2Int[] offsets = { Vector2Int.right, Vector2Int.up };
            foreach (var off in offsets)
            {
                if (nodes.TryGetValue(pos + off, out RepairPuzzleNode neighbor))
                    return Vector3.Distance(pair.Value.transform.position, neighbor.transform.position);
            }
        }
        return 1f;
    }

    private void FindSpecialNodes()
    {
        startA = null; endA = null;
        startB = null; endB = null;

        foreach (var pair in nodes)
        {
            switch (pair.Value.nodeType)
            {
                case RepairPuzzleNodeType.StartA: startA = pair.Value; break;
                case RepairPuzzleNodeType.EndA: endA = pair.Value; break;
                case RepairPuzzleNodeType.StartB: startB = pair.Value; break;
                case RepairPuzzleNodeType.EndB: endB = pair.Value; break;
            }
        }
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    public void ResetPuzzle()
    {
        // Destrói LineRenderers de portais criados em runtime
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i].lineRenderer != null &&
                segments[i].lineRenderer != wireRendererA &&
                segments[i].lineRenderer != wireRendererB)
            {
                Destroy(segments[i].lineRenderer.gameObject);
            }
        }

        foreach (var kv in portalGhosts)
            if (kv.Value != null) Destroy(kv.Value.gameObject);

        portalGhosts.Clear();
        portalColorIndex.Clear();

        segments.Clear();
        activeSegmentIndex = 0;
        isDragging = false;

        if (wireTiler != null)
            wireTiler.ClearTiles();

        SpawnPortalGhosts();

        WireSegment segA = new WireSegment();
        segA.expectedEnd = endA;
        segA.lineRenderer = wireRendererA;
        segments.Add(segA);

        if (difficulty == RepairPuzzleDifficulty.Difficulty4)
        {
            WireSegment segB = new WireSegment();
            segB.expectedEnd = endB;
            segB.lineRenderer = wireRendererB;
            segments.Add(segB);
        }

        RefreshVisuals();
    }

    // ── Drag / Path ───────────────────────────────────────────────────────────

    private void TryStartDrag(RepairPuzzleNode node)
    {
        if (node == null) return;

        WireSegment active = segments[activeSegmentIndex];

        if (active.complete) return;

        if (active.path.Count == 0)
        {
            RepairPuzzleNode expectedStart = GetExpectedStart(activeSegmentIndex);

            if (node != expectedStart)
                return;

            active.path.Add(node);
            isDragging = true;
            RefreshVisuals();
            return;
        }

        if (node == active.path[active.path.Count - 1])
            isDragging = true;
    }

    private RepairPuzzleNode GetExpectedStart(int segmentIndex)
    {
        if (segmentIndex == 0) return startA;

        if (difficulty == RepairPuzzleDifficulty.Difficulty4 && segmentIndex == 1)
            return startB;

        return segments[segmentIndex].path.Count > 0 ? segments[segmentIndex].path[0] : null;
    }

    private void TryExtendPath(RepairPuzzleNode targetNode)
    {
        WireSegment active = segments[activeSegmentIndex];

        if (active.complete) return;

        RepairPuzzleNode currentEnd = active.path.Count > 0
            ? active.path[active.path.Count - 1]
            : null;

        if (currentEnd == null || targetNode == null) return;
        if (targetNode == currentEnd) return;
        if (!AreOrthogonallyAdjacent(currentEnd, targetNode)) return;

        // Backtrack
        if (active.path.Count >= 2 && targetNode == active.path[active.path.Count - 2])
        {
            active.path.RemoveAt(active.path.Count - 1);
            RefreshVisuals();
            return;
        }

        if (IsNodeInAnyPath(targetNode)) return;

        if (targetNode.nodeType == RepairPuzzleNodeType.StartA ||
            targetNode.nodeType == RepairPuzzleNodeType.StartB)
            return;

        bool isExpectedEnd = (targetNode == active.expectedEnd);

        if (isExpectedEnd)
        {
            if (IsDangerForStep(targetNode))
            {
                FailPuzzle();
                return;
            }
        }
        else
        {
            if (targetNode.nodeType == RepairPuzzleNodeType.EndA && active.expectedEnd != endA) return;
            if (targetNode.nodeType == RepairPuzzleNodeType.EndB && active.expectedEnd != endB) return;

            if (!targetNode.IsWalkable())
            {
                if (targetNode.nodeType == RepairPuzzleNodeType.RedHazard)
                    FailPuzzle();
                return;
            }

            if (IsDangerForStep(targetNode))
            {
                FailPuzzle();
                return;
            }
        }

        active.path.Add(targetNode);

        if (targetNode.nodeType == RepairPuzzleNodeType.Portal && targetNode.portalTarget != null)
        {
            HandlePortalEntry(targetNode);
            return;
        }

        RefreshVisuals();
        CheckCompletion();
    }

    // ── Portal ────────────────────────────────────────────────────────────────

    private void SpawnPortalGhosts()
    {
        int pairIndex = 0;

        foreach (var pair in nodes)
        {
            RepairPuzzleNode entry = pair.Value;

            if (entry.nodeType != RepairPuzzleNodeType.Portal) continue;
            if (entry.portalTarget == null) continue;
            if (portalGhosts.ContainsKey(entry)) continue;

            int colorIdx = pairIndex % portalWireColors.Length;
            portalColorIndex[entry] = colorIdx;
            pairIndex++;

            Vector3[] pts = BuildGhostLPath(entry.transform.position, entry.portalTarget.transform.position);

            GameObject ghostObj = new GameObject("PortalGhost_" + entry.name);
            ghostObj.transform.SetParent(transform);

            RepairPuzzlePortalGhost ghost = ghostObj.AddComponent<RepairPuzzlePortalGhost>();

            int layerID = wireRendererA != null ? wireRendererA.sortingLayerID : 0;
            int order = wireRendererA != null ? wireRendererA.sortingOrder - 1 : 0;

            ghost.Setup(pts, layerID, order);
            portalGhosts[entry] = ghost;
        }
    }

    private Vector3[] BuildGhostLPath(Vector3 entryWorld, Vector3 exitWorld)
    {
        float z = wireZOffset - 0.05f;

        Vector3 entry = new Vector3(entryWorld.x, entryWorld.y, z);
        Vector3 exit = new Vector3(exitWorld.x, exitWorld.y, z);

        float left = gridBounds.min.x;
        float right = gridBounds.max.x;
        float bottom = gridBounds.min.y;
        float top = gridBounds.max.y;

        float midX = (entry.x + exit.x) * 0.5f;
        float midY = (entry.y + exit.y) * 0.5f;

        float dLeft = Mathf.Min(entry.x, exit.x) - left;
        float dRight = right - Mathf.Max(entry.x, exit.x);
        float dBottom = Mathf.Min(entry.y, exit.y) - bottom;
        float dTop = top - Mathf.Max(entry.y, exit.y);

        float minDist = Mathf.Min(dLeft, dRight, dBottom, dTop);

        Vector3 elbow;

        if (minDist == dLeft) elbow = new Vector3(left, midY, z);
        else if (minDist == dRight) elbow = new Vector3(right, midY, z);
        else if (minDist == dBottom) elbow = new Vector3(midX, bottom, z);
        else elbow = new Vector3(midX, top, z);

        return new Vector3[] { entry, elbow, exit };
    }

    private void HandlePortalEntry(RepairPuzzleNode portalEntry)
    {
        RepairPuzzleNode portalExit = portalEntry.portalTarget;

        if (portalExit == null)
        {
            RefreshVisuals();
            return;
        }

        if (IsDangerForStep(portalExit))
        {
            FailPuzzle();
            return;
        }

        if (portalGhosts.TryGetValue(portalEntry, out RepairPuzzlePortalGhost ghost) && ghost != null)
        {
            int colorIdx = portalColorIndex.ContainsKey(portalEntry) ? portalColorIndex[portalEntry] : 0;
            ghost.Activate(portalWireColors[colorIdx % portalWireColors.Length]);
        }

        segments[activeSegmentIndex].complete = true;
        isDragging = false;

        RepairPuzzleNode inheritedEnd = segments[activeSegmentIndex].expectedEnd;

        WireSegment newSegment = new WireSegment();
        newSegment.path.Add(portalExit);
        newSegment.expectedEnd = inheritedEnd;
        newSegment.lineRenderer = CreatePortalLineRenderer();
        segments.Add(newSegment);

        activeSegmentIndex = segments.Count - 1;

        Debug.Log("[Portal] Entrada conectada. Novo segmento ativado na saída: " + portalExit.name);

        RefreshVisuals();
        CheckCompletion();
    }

    private LineRenderer CreatePortalLineRenderer()
    {
        // LR vazio — o desenho real é feito pelo WireTiler.
        int count = 0;

        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i].lineRenderer != wireRendererA &&
                segments[i].lineRenderer != wireRendererB)
                count++;
        }

        GameObject obj = new GameObject("PortalWire_" + count);
        obj.transform.SetParent(transform);

        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.positionCount = 0;

        return lr;
    }

    // ── Completion ────────────────────────────────────────────────────────────

    private void CheckCompletion()
    {
        WireSegment active = segments[activeSegmentIndex];

        if (active.path.Count == 0) return;

        RepairPuzzleNode last = active.path[active.path.Count - 1];

        if (active.expectedEnd != null && last == active.expectedEnd)
        {
            active.complete = true;
            isDragging = false;

            Debug.Log("[Runtime] Segmento " + activeSegmentIndex + " completo.");

            if (AllSegmentsComplete())
            {
                SuccessPuzzle();
                return;
            }

            for (int i = 0; i < segments.Count; i++)
            {
                if (!segments[i].complete)
                {
                    activeSegmentIndex = i;
                    break;
                }
            }

            RefreshVisuals();
        }
    }

    private bool AllSegmentsComplete()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            if (!segments[i].complete)
                return false;
        }

        return true;
    }

    // ── Helpers de path ───────────────────────────────────────────────────────

    private bool IsNodeInAnyPath(RepairPuzzleNode node)
    {
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i].path.Contains(node))
                return true;
        }

        return false;
    }

    private bool IsDangerForStep(RepairPuzzleNode node)
    {
        if (difficulty == RepairPuzzleDifficulty.Difficulty1) return false;
        if (node == null) return false;
        if (node.nodeType == RepairPuzzleNodeType.RedHazard) return true;

        Vector2Int pos = new Vector2Int(node.x, node.y);
        Vector2Int[] offsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        for (int i = 0; i < offsets.Length; i++)
        {
            if (nodes.TryGetValue(pos + offsets[i], out RepairPuzzleNode neighbor))
            {
                if (neighbor != null && neighbor.nodeType == RepairPuzzleNodeType.RedHazard)
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

    // ── Visuais ───────────────────────────────────────────────────────────────

    private void RefreshVisuals()
    {
        // Reseta cor de todos os nós
        foreach (var pair in nodes)
            pair.Value.SetNormal();

        // Marca nós no path
        for (int i = 0; i < segments.Count; i++)
        {
            for (int j = 0; j < segments[i].path.Count; j++)
                segments[i].path[j].SetPath();
        }

        if (startA != null) startA.SetPath();
        if (endA != null) endA.SetPath();

        if (difficulty == RepairPuzzleDifficulty.Difficulty4)
        {
            bool segAComplete = segments.Count > 0 && segments[0].complete;

            if (segAComplete)
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

        // Zera LineRenderers (mantidos apenas por compatibilidade)
        if (wireRendererA != null) wireRendererA.positionCount = 0;
        if (wireRendererB != null) wireRendererB.positionCount = 0;

        // WireTiler: reconstrói todos os sprites de fio
        if (wireTiler != null)
        {
            wireTiler.ClearTiles();

            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i].path.Count == 0) continue;
                wireTiler.BuildTiles(segments[i].path, Color.white);
            }
        }
    }

    // ── Input helpers ─────────────────────────────────────────────────────────

    private Vector3 GetPointerWorldPosition()
    {
        if (puzzleCamera == null || Mouse.current == null)
            return Vector3.zero;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = puzzleCamera.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, Mathf.Abs(puzzleCamera.transform.position.z))
        );

        worldPos.z = wireZOffset;
        return worldPos;
    }

    private RepairPuzzleNode GetNodeUnderPointer()
    {
        if (puzzleCamera == null || Mouse.current == null) return null;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = puzzleCamera.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, Mathf.Abs(puzzleCamera.transform.position.z))
        );

        Collider2D[] hits = Physics2D.OverlapPointAll(new Vector2(worldPos.x, worldPos.y));

        if (hits == null || hits.Length == 0) return null;

        for (int i = 0; i < hits.Length; i++)
        {
            RepairPuzzleNode node = hits[i].GetComponent<RepairPuzzleNode>();
            if (node != null && node.gameObject.scene == gameObject.scene)
                return node;
        }

        return null;
    }

    // ── API pública ───────────────────────────────────────────────────────────

    public List<RepairPuzzleNode> GetNodesByType(RepairPuzzleNodeType type)
    {
        List<RepairPuzzleNode> result = new List<RepairPuzzleNode>();

        foreach (var pair in nodes)
        {
            if (pair.Value != null && pair.Value.nodeType == type)
                result.Add(pair.Value);
        }

        return result;
    }

    public void FailPuzzle()
    {
        Debug.Log("PUZZLE FAIL");

        if (DungeonAlertSystem.Instance != null)
            DungeonAlertSystem.Instance.RegisterMinigameFailure();

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