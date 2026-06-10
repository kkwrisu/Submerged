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
    public RepairPuzzleWireTiler wireTiler;

    [Header("Portal Wire Visual")]
    public float portalWireWidth = 0.1f;
    public Color[] portalWireColors = new Color[]
    {
        new Color(0.2f, 0.8f, 1f),
        new Color(1f, 0.5f, 0f),
        new Color(0.8f, 0.2f, 1f),
        new Color(0.2f, 1f, 0.5f),
    };

    [Header("Fail Animation")]
    [Tooltip("Pausa (segundos) entre o hazard aparecer e o fio começar a quebrar.")]
    public float failHazardToWireDelay = 0.15f;
    [Tooltip("Pausa (segundos) após o fio quebrar antes de fechar o puzzle.")]
    public float failAfterBreakDelay = 0.3f;
    [Tooltip("SFX tocado quando o fio quebra no hazard.")]
    public AudioClip failWireBreakSFX;

    [Header("Success Animation")]
    [Tooltip("Sprite verde do EndA exibido ao concluir.")]
    public Sprite endSuccessSprite;
    [Tooltip("Quantas vezes o EndA pisca ao concluir.")]
    public int successBlinkCount = 3;
    [Tooltip("Duração de cada piscada (segundos).")]
    public float successBlinkInterval = 0.12f;
    [Tooltip("SFX tocado ao concluir o puzzle.")]
    public AudioClip successSFX;

    private readonly Dictionary<Vector2Int, RepairPuzzleNode> nodes =
        new Dictionary<Vector2Int, RepairPuzzleNode>();

    private RepairPuzzleNode startA;
    private RepairPuzzleNode endA;
    private RepairPuzzleNode startB;
    private RepairPuzzleNode endB;

    private class WireSegment
    {
        public List<RepairPuzzleNode> path = new List<RepairPuzzleNode>();
        public RepairPuzzleNode expectedEnd;
        public LineRenderer lineRenderer;
        public bool complete;
    }

    private readonly List<WireSegment> segments = new List<WireSegment>();
    private int activeSegmentIndex = 0;
    private bool isDragging;

    private readonly Dictionary<RepairPuzzleNode, RepairPuzzlePortalGhost> portalGhosts =
        new Dictionary<RepairPuzzleNode, RepairPuzzlePortalGhost>();

    private readonly Dictionary<RepairPuzzleNode, int> portalColorIndex =
        new Dictionary<RepairPuzzleNode, int>();

    private Bounds gridBounds;
    private float nodeSpacing = 1f;
    private bool lockedByTutorial;
    private bool isFailing;
    private bool isSucceeding;

    private InputAction click;
    private InputAction reset;

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

    private void Update()
    {
        if (RepairPuzzleManager.Instance == null || !RepairPuzzleManager.Instance.IsPuzzleOpen())
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            PauseMenu.Instance?.BlockPauseForOneFrame();
            RepairPuzzleManager.Instance.FinishPuzzle(RepairPuzzleResult.None);
            return;
        }

        if (isFailing || isSucceeding || lockedByTutorial)
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

    private Camera FindPuzzleCameraInMyScene()
    {
        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

        for (int i = 0; i < allCameras.Length; i++)
            if (allCameras[i].gameObject.scene == gameObject.scene)
                return allCameras[i];

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
            nodeSpacing = GetNodeSpacing();
            gridBounds.Expand(nodeSpacing);
        }
    }

    private float GetNodeSpacing()
    {
        foreach (var pair in nodes)
        {
            Vector2Int pos = new Vector2Int(pair.Value.x, pair.Value.y);
            Vector2Int[] offsets = { Vector2Int.right, Vector2Int.up };
            foreach (var off in offsets)
                if (nodes.TryGetValue(pos + off, out RepairPuzzleNode neighbor))
                    return Vector3.Distance(pair.Value.transform.position, neighbor.transform.position);
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

    public void ResetPuzzle()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i].lineRenderer != null &&
                segments[i].lineRenderer != wireRendererA &&
                segments[i].lineRenderer != wireRendererB)
            {
                Destroy(segments[i].lineRenderer.gameObject);
            }
        }

        segments.Clear();
        activeSegmentIndex = 0;
        isDragging = false;
        isFailing = false;
        isSucceeding = false;

        if (wireTiler != null)
            wireTiler.ClearAll();

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

    private void TryStartDrag(RepairPuzzleNode node)
    {
        if (node == null) return;

        WireSegment active = segments[activeSegmentIndex];
        if (active.complete) return;

        if (active.path.Count == 0)
        {
            RepairPuzzleNode expectedStart = GetExpectedStart(activeSegmentIndex);
            if (node != expectedStart) return;

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
        if (difficulty == RepairPuzzleDifficulty.Difficulty4 && segmentIndex == 1) return startB;
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
                TriggerFailSequence(targetNode);
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
                    TriggerFailSequence(targetNode);
                return;
            }

            if (IsDangerForStep(targetNode))
            {
                TriggerFailSequence(targetNode);
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

    private void TriggerFailSequence(RepairPuzzleNode triggerHazard)
    {
        if (isFailing) return;
        isFailing = true;
        isDragging = false;

        RepairPuzzleNode hazardNode = FindAdjacentHazard(triggerHazard) ?? triggerHazard;
        StartCoroutine(FailSequenceRoutine(hazardNode));
    }

    private IEnumerator FailSequenceRoutine(RepairPuzzleNode hazardNode)
    {
        if (failWireBreakSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(failWireBreakSFX);

        bool hazardDone = false;
        if (hazardNode != null)
            hazardNode.PlayHazardReveal(() => hazardDone = true);
        else
            hazardDone = true;

        while (!hazardDone)
            yield return null;

        float wait = failHazardToWireDelay;
        while (wait > 0f) { wait -= Time.unscaledDeltaTime; yield return null; }

        bool wireDone = false;
        if (wireTiler != null)
            wireTiler.PlayBreakAnimationAll(() => wireDone = true);
        else
            wireDone = true;

        while (!wireDone)
            yield return null;

        float afterBreak = failAfterBreakDelay;
        while (afterBreak > 0f) { afterBreak -= Time.unscaledDeltaTime; yield return null; }

        FailPuzzle();
    }

    private RepairPuzzleNode FindAdjacentHazard(RepairPuzzleNode node)
    {
        if (node == null) return null;
        if (node.nodeType == RepairPuzzleNodeType.RedHazard) return node;

        Vector2Int pos = new Vector2Int(node.x, node.y);
        Vector2Int[] offsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        for (int i = 0; i < offsets.Length; i++)
            if (nodes.TryGetValue(pos + offsets[i], out RepairPuzzleNode neighbor))
                if (neighbor != null && neighbor.nodeType == RepairPuzzleNodeType.RedHazard)
                    return neighbor;

        return null;
    }

    private void TriggerSuccessSequence()
    {
        if (isSucceeding) return;
        isSucceeding = true;
        isDragging = false;

        StartCoroutine(SuccessSequenceRoutine());
    }

    private IEnumerator SuccessSequenceRoutine()
    {
        if (successSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(successSFX);

        if (endA != null && endA.spriteRenderer != null && endSuccessSprite != null)
        {
            Sprite originalSprite = endA.spriteRenderer.sprite;

            for (int i = 0; i < successBlinkCount; i++)
            {
                endA.spriteRenderer.sprite = endSuccessSprite;
                float t = 0f;
                while (t < successBlinkInterval) { t += Time.unscaledDeltaTime; yield return null; }

                endA.spriteRenderer.sprite = originalSprite;
                t = 0f;
                while (t < successBlinkInterval) { t += Time.unscaledDeltaTime; yield return null; }
            }

            endA.spriteRenderer.sprite = endSuccessSprite;
        }

        float afterSuccess = successBlinkInterval * 2f;
        while (afterSuccess > 0f) { afterSuccess -= Time.unscaledDeltaTime; yield return null; }

        SuccessPuzzle();
    }

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
            TriggerFailSequence(portalExit);
            return;
        }

        if (portalGhosts.TryGetValue(portalEntry, out RepairPuzzlePortalGhost ghost) && ghost != null)
        {
            int colorIdx = portalColorIndex.ContainsKey(portalEntry) ? portalColorIndex[portalEntry] : 0;
            ghost.Activate(portalWireColors[colorIdx % portalWireColors.Length]);
        }

        if (wireTiler != null)
        {
            bool isOriginalPortalSegment = (activeSegmentIndex == 0 ||
                (difficulty == RepairPuzzleDifficulty.Difficulty4 && activeSegmentIndex == 1));
            wireTiler.RebuildSegment(activeSegmentIndex, segments[activeSegmentIndex].path,
                                     nodeSpacing, isOriginalPortalSegment, false);
        }

        segments[activeSegmentIndex].complete = true;
        isDragging = true;

        RepairPuzzleNode inheritedEnd = segments[activeSegmentIndex].expectedEnd;

        WireSegment newSegment = new WireSegment();
        newSegment.path.Add(portalExit);
        newSegment.expectedEnd = inheritedEnd;
        newSegment.lineRenderer = CreatePortalLineRenderer();
        segments.Add(newSegment);

        activeSegmentIndex = segments.Count - 1;

        RefreshVisuals();
    }

    private LineRenderer CreatePortalLineRenderer()
    {
        int count = 0;
        for (int i = 0; i < segments.Count; i++)
            if (segments[i].lineRenderer != wireRendererA && segments[i].lineRenderer != wireRendererB)
                count++;

        GameObject obj = new GameObject("PortalWire_" + count);
        obj.transform.SetParent(transform);

        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.positionCount = 0;
        return lr;
    }

    private void CheckCompletion()
    {
        WireSegment active = segments[activeSegmentIndex];
        if (active.path.Count == 0) return;

        RepairPuzzleNode last = active.path[active.path.Count - 1];

        if (active.expectedEnd != null && last == active.expectedEnd)
        {
            active.complete = true;
            isDragging = false;

            if (AllSegmentsComplete())
            {
                TriggerSuccessSequence();
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
            if (!segments[i].complete) return false;
        return true;
    }

    private bool IsNodeInAnyPath(RepairPuzzleNode node)
    {
        for (int i = 0; i < segments.Count; i++)
            if (segments[i].path.Contains(node)) return true;
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
            if (nodes.TryGetValue(pos + offsets[i], out RepairPuzzleNode neighbor))
                if (neighbor != null && neighbor.nodeType == RepairPuzzleNodeType.RedHazard)
                    return true;

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
        if (wireRendererA != null) wireRendererA.positionCount = 0;
        if (wireRendererB != null) wireRendererB.positionCount = 0;
        if (wireTiler == null) return;

        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i].complete) continue;

            bool isOriginalSegment = (i == 0 || (difficulty == RepairPuzzleDifficulty.Difficulty4 && i == 1));

            List<RepairPuzzleNode> path = segments[i].path;
            bool skipLast = path.Count > 0 &&
                            (path[path.Count - 1].nodeType == RepairPuzzleNodeType.EndA ||
                             path[path.Count - 1].nodeType == RepairPuzzleNodeType.EndB);

            wireTiler.RebuildSegment(i, path, nodeSpacing, isOriginalSegment, skipLast);
        }
    }

    private RepairPuzzleNode GetNodeUnderPointer()
    {
        if (puzzleCamera == null || Mouse.current == null) return null;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = puzzleCamera.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, Mathf.Abs(puzzleCamera.transform.position.z)));

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

    public List<RepairPuzzleNode> GetNodesByType(RepairPuzzleNodeType type)
    {
        List<RepairPuzzleNode> result = new List<RepairPuzzleNode>();

        foreach (var pair in nodes)
            if (pair.Value != null && pair.Value.nodeType == type)
                result.Add(pair.Value);

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