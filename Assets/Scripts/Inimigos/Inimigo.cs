using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class Inimigo : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Suspicious,
        Investigate,
        Chase,
        Catch,
        Search,
        ReturnToPatrol
    }

    [Header("References")]
    public NavMeshAgent agent;
    public Transform player;
    public Player_Status playerStealth;
    public Transform eyePoint;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolWaitTime = 2f;
    public float patrolPointReachDistance = 0.8f;

    [Header("Vision")]
    public float viewDistance = 12f;
    [Range(0f, 360f)] public float viewAngle = 90f;
    public LayerMask obstructionMask = ~0;
    public float suspiciousViewMultiplier = 1.35f;
    public float crouchVisionMultiplier = 0.55f;
    public float sprintVisionMultiplier = 1.25f;
    public float detectionBuildSpeed = 1.4f;
    public float detectionFallSpeed = 1.1f;
    public float detectionThreshold = 1f;

    [Header("Hearing")]
    public float hearingRadius = 7f;
    public float hearingThreshold = 0.2f;
    public float investigateSoundDuration = 2.5f;

    [Header("Movement Speeds")]
    public float chaseSpeed = 4.5f;
    public float patrolSpeed = 2f;
    public float suspiciousSpeed = 2.6f;
    public float searchSpeed = 3f;
    public float lostSightGraceTime = 2f;

    [Header("Search")]
    public float searchDuration = 5f;
    public float searchRadius = 3.5f;
    public float searchPointInterval = 1.2f;

    [Header("Catch")]
    public float catchRange = 1.8f;
    public float catchCooldown = 1.2f;

    [Header("Audio")]
    public AudioSource detectAudio;
    public AudioSource chaseLoopAudio;

    [Header("Events")]
    public UnityEvent onPlayerDetected;
    public UnityEvent onPlayerLost;
    public UnityEvent onPlayerCaught;

    [Header("Debug")]
    public EnemyState currentState = EnemyState.Patrol;
    [Range(0f, 1f)] public float detectionMeter;

    private int currentPatrolIndex;
    private float patrolWaitTimer;
    private float lostSightTimer;
    private float catchTimer;
    private float searchTimer;
    private float searchPointTimer;
    private float suspiciousTimer;

    private Vector3 lastKnownPlayerPosition;
    private Vector3 heardPosition;
    private bool hasHeardSomething;
    private bool hadPlayerInSightLastFrame;

    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (eyePoint == null)
            eyePoint = transform;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer != null)
                player = foundPlayer.transform;
        }

        if (playerStealth == null && player != null)
            playerStealth = player.GetComponent<Player_Status>();

        SetSpeedForState();

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            GoToCurrentPatrolPoint();
    }

    private void Update()
    {
        if (player == null || agent == null)
            return;

        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        if (catchTimer > 0f)
            catchTimer -= Time.deltaTime;

        bool canSeePlayer = CanSeePlayer();
        bool canHearPlayer = CanHearPlayer();

        UpdateDetectionMeter(canSeePlayer);

        switch (currentState)
        {
            case EnemyState.Patrol:
                UpdatePatrol(canSeePlayer, canHearPlayer);
                break;

            case EnemyState.Suspicious:
                UpdateSuspicious(canSeePlayer, canHearPlayer);
                break;

            case EnemyState.Investigate:
                UpdateInvestigate(canSeePlayer);
                break;

            case EnemyState.Chase:
                UpdateChase(canSeePlayer, canHearPlayer);
                break;

            case EnemyState.Catch:
                UpdateCatch(canSeePlayer);
                break;

            case EnemyState.Search:
                UpdateSearch(canSeePlayer, canHearPlayer);
                break;

            case EnemyState.ReturnToPatrol:
                UpdateReturnToPatrol(canSeePlayer, canHearPlayer);
                break;
        }

        hadPlayerInSightLastFrame = canSeePlayer;
    }

    private void UpdatePatrol(bool canSeePlayer, bool canHearPlayer)
    {
        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        if (canSeePlayer && detectionMeter >= detectionThreshold)
        {
            EnterChase();
            return;
        }

        if (canHearPlayer)
        {
            EnterSuspicious(GetPlayerPosition());
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        if (!agent.pathPending && agent.remainingDistance <= patrolPointReachDistance)
        {
            patrolWaitTimer += Time.deltaTime;

            if (patrolWaitTimer >= patrolWaitTime)
            {
                patrolWaitTimer = 0f;
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                GoToCurrentPatrolPoint();
            }
        }
    }

    private void UpdateSuspicious(bool canSeePlayer, bool canHearPlayer)
    {
        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        if (canSeePlayer && detectionMeter >= detectionThreshold)
        {
            EnterChase();
            return;
        }

        suspiciousTimer += Time.deltaTime;

        if (canHearPlayer)
        {
            heardPosition = GetPlayerPosition();
            hasHeardSomething = true;
            suspiciousTimer = 0f;
            SetDestination(heardPosition);
        }

        if (!agent.pathPending && agent.remainingDistance <= 1f)
        {
            EnterInvestigate(hasHeardSomething ? heardPosition : lastKnownPlayerPosition);
            return;
        }

        if (suspiciousTimer >= investigateSoundDuration)
        {
            EnterInvestigate(hasHeardSomething ? heardPosition : transform.position);
        }
    }

    private void UpdateInvestigate(bool canSeePlayer)
    {
        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        if (canSeePlayer && detectionMeter >= detectionThreshold)
        {
            EnterChase();
            return;
        }

        searchTimer += Time.deltaTime;
        searchPointTimer += Time.deltaTime;

        if (searchPointTimer >= searchPointInterval)
        {
            searchPointTimer = 0f;
            Vector3 randomPoint = GetRandomPointNear(lastKnownPlayerPosition, searchRadius);
            SetDestination(randomPoint);
        }

        if (searchTimer >= searchDuration)
        {
            EnterReturnToPatrol();
        }
    }

    private void UpdateChase(bool canSeePlayer, bool canHearPlayer)
    {
        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        SetDestination(GetPlayerPosition());

        float distanceToPlayer = Vector3.Distance(transform.position, GetPlayerPosition());

        if (distanceToPlayer <= catchRange)
        {
            EnterCatch();
            return;
        }

        if (canSeePlayer)
        {
            lastKnownPlayerPosition = GetPlayerPosition();
            lostSightTimer = 0f;
        }
        else
        {
            if (canHearPlayer)
                lastKnownPlayerPosition = GetPlayerPosition();

            lostSightTimer += Time.deltaTime;

            if (lostSightTimer >= lostSightGraceTime)
            {
                EnterSearch(lastKnownPlayerPosition);
            }
        }
    }

    private void UpdateCatch(bool canSeePlayer)
    {
        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = true;

        Vector3 lookTarget = GetPlayerPosition();
        lookTarget.y = transform.position.y;
        Vector3 dir = (lookTarget - transform.position).normalized;

        if (dir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }

        float distanceToPlayer = Vector3.Distance(transform.position, GetPlayerPosition());

        if (distanceToPlayer > catchRange + 0.4f)
        {
            agent.isStopped = false;
            EnterChase();
            return;
        }

        if (catchTimer <= 0f)
        {
            CatchPlayer();
            catchTimer = catchCooldown;
        }

        if (!canSeePlayer && distanceToPlayer > catchRange)
        {
            agent.isStopped = false;
            EnterSearch(lastKnownPlayerPosition);
        }
    }

    private void UpdateSearch(bool canSeePlayer, bool canHearPlayer)
    {
        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        if (canSeePlayer && detectionMeter >= detectionThreshold)
        {
            EnterChase();
            return;
        }

        if (canHearPlayer)
        {
            EnterSuspicious(GetPlayerPosition());
            return;
        }

        searchTimer += Time.deltaTime;
        searchPointTimer += Time.deltaTime;

        if (searchPointTimer >= searchPointInterval)
        {
            searchPointTimer = 0f;
            Vector3 randomPoint = GetRandomPointNear(lastKnownPlayerPosition, searchRadius);
            SetDestination(randomPoint);
        }

        if (searchTimer >= searchDuration)
        {
            EnterReturnToPatrol();
        }
    }

    private void UpdateReturnToPatrol(bool canSeePlayer, bool canHearPlayer)
    {
        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        if (canSeePlayer && detectionMeter >= detectionThreshold)
        {
            EnterChase();
            return;
        }

        if (canHearPlayer)
        {
            EnterSuspicious(GetPlayerPosition());
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        if (!agent.pathPending && agent.remainingDistance <= patrolPointReachDistance)
        {
            ChangeState(EnemyState.Patrol);
            GoToCurrentPatrolPoint();
        }
    }

    private void UpdateDetectionMeter(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            float multiplier = 1f;

            if (playerStealth != null)
            {
                if (playerStealth.isCrouching)
                    multiplier *= 0.7f;

                if (playerStealth.isSprinting)
                    multiplier *= 1.35f;
            }

            detectionMeter += detectionBuildSpeed * multiplier * Time.deltaTime;
        }
        else
        {
            detectionMeter -= detectionFallSpeed * Time.deltaTime;
        }

        detectionMeter = Mathf.Clamp01(detectionMeter);
    }

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector3 origin = eyePoint != null ? eyePoint.position : transform.position + Vector3.up * 1.6f;
        Vector3 target = player.position + Vector3.up * 1.0f;
        Vector3 toPlayer = target - origin;

        float distance = toPlayer.magnitude;
        float adjustedViewDistance = viewDistance;

        if (currentState == EnemyState.Suspicious || currentState == EnemyState.Investigate)
            adjustedViewDistance *= suspiciousViewMultiplier;

        if (playerStealth != null)
        {
            if (playerStealth.isCrouching)
                adjustedViewDistance *= crouchVisionMultiplier;

            if (playerStealth.isSprinting)
                adjustedViewDistance *= sprintVisionMultiplier;
        }

        if (distance > adjustedViewDistance)
            return false;

        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > viewAngle * 0.5f)
            return false;

        if (Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, distance, obstructionMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == player || hit.transform.IsChildOf(player))
            {
                lastKnownPlayerPosition = player.position;

                if (!hadPlayerInSightLastFrame)
                    onPlayerDetected?.Invoke();

                return true;
            }
        }

        return false;
    }

    private bool CanHearPlayer()
    {
        if (player == null || playerStealth == null)
            return false;

        float noise = playerStealth.CurrentNoise;
        if (noise < hearingThreshold)
            return false;

        float distance = Vector3.Distance(transform.position, player.position);
        float effectiveRadius = hearingRadius * Mathf.Lerp(0.5f, 1.5f, noise);

        if (distance <= effectiveRadius)
        {
            heardPosition = player.position;
            hasHeardSomething = true;
            return true;
        }

        return false;
    }

    private void EnterSuspicious(Vector3 targetPosition)
    {
        hasHeardSomething = true;
        heardPosition = targetPosition;
        suspiciousTimer = 0f;

        ChangeState(EnemyState.Suspicious);
        SetDestination(targetPosition);
    }

    private void EnterInvestigate(Vector3 targetPosition)
    {
        lastKnownPlayerPosition = targetPosition;
        searchTimer = 0f;
        searchPointTimer = 0f;

        ChangeState(EnemyState.Investigate);
        SetDestination(targetPosition);
    }

    private void EnterChase()
    {
        bool wasAlreadyChasing = currentState == EnemyState.Chase;

        ChangeState(EnemyState.Chase);
        SetDestination(GetPlayerPosition());

        if (!wasAlreadyChasing)
        {
            if (detectAudio != null)
                detectAudio.Play();

            if (chaseLoopAudio != null && !chaseLoopAudio.isPlaying)
                chaseLoopAudio.Play();
        }
    }

    private void EnterCatch()
    {
        StopChaseAudio();
        ChangeState(EnemyState.Catch);
    }

    private void EnterSearch(Vector3 targetPosition)
    {
        StopChaseAudio();
        onPlayerLost?.Invoke();

        lastKnownPlayerPosition = targetPosition;
        searchTimer = 0f;
        searchPointTimer = 0f;

        ChangeState(EnemyState.Search);
        SetDestination(targetPosition);
    }

    private void EnterReturnToPatrol()
    {
        StopChaseAudio();
        ChangeState(EnemyState.ReturnToPatrol);

        if (patrolPoints != null && patrolPoints.Length > 0)
            SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    private void ChangeState(EnemyState newState)
    {
        currentState = newState;
        SetSpeedForState();

        if (currentState != EnemyState.Catch)
            agent.isStopped = false;
    }

    private void SetSpeedForState()
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
            case EnemyState.ReturnToPatrol:
                agent.speed = patrolSpeed;
                break;

            case EnemyState.Suspicious:
            case EnemyState.Investigate:
                agent.speed = suspiciousSpeed;
                break;

            case EnemyState.Search:
                agent.speed = searchSpeed;
                break;

            case EnemyState.Chase:
            case EnemyState.Catch:
                agent.speed = chaseSpeed;
                break;
        }
    }

    private void GoToCurrentPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    private void SetDestination(Vector3 position)
    {
        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = false;
        agent.SetDestination(position);
    }

    private Vector3 GetPlayerPosition()
    {
        return player != null ? player.position : transform.position;
    }

    private Vector3 GetRandomPointNear(Vector3 center, float radius)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 random = center + new Vector3(Random.Range(-radius, radius), 0f, Random.Range(-radius, radius));

            if (NavMesh.SamplePosition(random, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                return hit.position;
        }

        return center;
    }

    private void CatchPlayer()
    {
        StopChaseAudio();
        onPlayerCaught?.Invoke();
        Debug.Log("Inimigo: Player capturado.");
    }

    private void StopChaseAudio()
    {
        if (chaseLoopAudio != null && chaseLoopAudio.isPlaying)
            chaseLoopAudio.Stop();
    }

    private void OnDrawGizmosSelected()
    {
        Transform originTransform = eyePoint != null ? eyePoint : transform;
        Vector3 origin = originTransform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);

        Vector3 leftBoundary = DirFromAngle(-viewAngle * 0.5f);
        Vector3 rightBoundary = DirFromAngle(viewAngle * 0.5f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, origin + leftBoundary * viewDistance);
        Gizmos.DrawLine(origin, origin + rightBoundary * viewDistance);

        Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
        Gizmos.DrawWireSphere(origin, viewDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchRange);

        if (player != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(origin, player.position + Vector3.up);
        }
    }

    private Vector3 DirFromAngle(float angleDegrees)
    {
        float rad = (transform.eulerAngles.y + angleDegrees) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
    }
}