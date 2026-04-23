using System.Collections;
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
    public float investigateDuration = 5f;
    public float searchDurationAfterChase = 9f;
    public float searchRadius = 3.5f;
    public float searchPointInterval = 1.2f;

    [Header("Catch")]
    public float catchRange = 1.8f;
    public float catchCooldown = 1.2f;
    public float respawnDelay = 0.15f;

    [Header("Respawn Reset")]
    public float postRespawnIgnoreDuration = 2f;

    [Header("Dungeon Alert")]
    public bool useDungeonAlert = true;
    [Range(0f, 100f)] public float alertIncreaseOnDetection = 15f;

    [Header("Audio")]
    public AudioSource detectAudio;
    public AudioSource chaseLoopAudio;
    public float chaseLoopStartDelay = 0.4f;
    public float chaseLoopFadeOutDuration = 0.8f;

    [Header("Detection Pause")]
    public float detectionPauseDuration = 0.35f;

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
    private float respawnTimer;
    private float detectionPauseTimer;
    private float currentSearchDuration;
    private float postRespawnIgnoreTimer;

    private Vector3 lastKnownPlayerPosition;
    private Vector3 heardPosition;
    private bool hasHeardSomething;
    private bool hadPlayerInSightLastFrame;
    private bool isHandlingCapture;
    private bool isInDetectionPause;
    private Coroutine chaseLoopStartRoutine;
    private Coroutine chaseLoopFadeRoutine;
    private float chaseLoopOriginalVolume = 1f;

    private float CurrentSpeedMultiplier
    {
        get
        {
            if (!useDungeonAlert || DungeonAlertSystem.Instance == null)
                return 1f;

            return DungeonAlertSystem.Instance.SpeedMultiplier;
        }
    }

    private float CurrentTimeMultiplier
    {
        get
        {
            if (!useDungeonAlert || DungeonAlertSystem.Instance == null)
                return 1f;

            return DungeonAlertSystem.Instance.TimeMultiplier;
        }
    }

    private float EffectivePatrolWaitTime => patrolWaitTime / CurrentTimeMultiplier;
    private float EffectiveInvestigateSoundDuration => investigateSoundDuration / CurrentTimeMultiplier;
    private float EffectiveInvestigateDuration => investigateDuration / CurrentTimeMultiplier;
    private float EffectiveSearchDurationAfterChase => searchDurationAfterChase / CurrentTimeMultiplier;
    private float EffectiveSearchPointInterval => searchPointInterval / CurrentTimeMultiplier;
    private float EffectiveLostSightGraceTime => lostSightGraceTime / CurrentTimeMultiplier;

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

        if (detectAudio != null)
            detectAudio.playOnAwake = false;

        if (chaseLoopAudio != null)
        {
            chaseLoopAudio.playOnAwake = false;
            chaseLoopOriginalVolume = chaseLoopAudio.volume;
        }

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

        if (postRespawnIgnoreTimer > 0f)
            postRespawnIgnoreTimer -= Time.deltaTime;

        if (isHandlingCapture)
        {
            respawnTimer -= Time.deltaTime;

            if (respawnTimer <= 0f)
            {
                DoRespawn();
                isHandlingCapture = false;
            }

            return;
        }

        if (isInDetectionPause)
        {
            detectionPauseTimer -= Time.deltaTime;
            agent.isStopped = true;

            if (detectionPauseTimer <= 0f)
            {
                isInDetectionPause = false;
                agent.isStopped = false;

                if (currentState == EnemyState.Chase)
                {
                    if (CanSeePlayer())
                        SetDestination(GetPlayerPosition());
                    else
                        SetDestination(lastKnownPlayerPosition);
                }
            }

            return;
        }

        if (catchTimer > 0f)
            catchTimer -= Time.deltaTime;

        bool canSeePlayer = postRespawnIgnoreTimer <= 0f && CanSeePlayer();
        bool canHearPlayer = postRespawnIgnoreTimer <= 0f && CanHearPlayer();

        UpdateDetectionMeter(canSeePlayer);
        SetSpeedForState();

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
            EnterSuspicious(heardPosition);
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        if (!agent.pathPending && agent.remainingDistance <= patrolPointReachDistance)
        {
            patrolWaitTimer += Time.deltaTime;

            if (patrolWaitTimer >= EffectivePatrolWaitTime)
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
            suspiciousTimer = 0f;

        if (!agent.pathPending && agent.remainingDistance <= 1f)
        {
            EnterInvestigate(hasHeardSomething ? heardPosition : lastKnownPlayerPosition);
            return;
        }

        if (suspiciousTimer >= EffectiveInvestigateSoundDuration)
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

        if (searchPointTimer >= EffectiveSearchPointInterval)
        {
            searchPointTimer = 0f;
            Vector3 randomPoint = GetRandomPointNear(lastKnownPlayerPosition, searchRadius);
            SetDestination(randomPoint);
        }

        if (searchTimer >= currentSearchDuration)
        {
            EnterReturnToPatrol();
        }
    }

    private void UpdateChase(bool canSeePlayer, bool canHearPlayer)
    {
        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        if (canSeePlayer)
        {
            lastKnownPlayerPosition = GetPlayerPosition();
            SetDestination(lastKnownPlayerPosition);
            lostSightTimer = 0f;
        }
        else if (canHearPlayer)
        {
            lastKnownPlayerPosition = heardPosition;
            SetDestination(lastKnownPlayerPosition);
            lostSightTimer = 0f;
        }
        else
        {
            lostSightTimer += Time.deltaTime;
            SetDestination(lastKnownPlayerPosition);

            if (lostSightTimer >= EffectiveLostSightGraceTime)
            {
                EnterSearch(lastKnownPlayerPosition);
                return;
            }
        }

        float distanceToPlayer = Vector3.Distance(transform.position, GetPlayerPosition());

        if (distanceToPlayer <= catchRange)
        {
            EnterCatch();
            return;
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
            EnterSuspicious(heardPosition);
            return;
        }

        searchTimer += Time.deltaTime;
        searchPointTimer += Time.deltaTime;

        if (searchPointTimer >= EffectiveSearchPointInterval)
        {
            searchPointTimer = 0f;
            Vector3 randomPoint = GetRandomPointNear(lastKnownPlayerPosition, searchRadius);
            SetDestination(randomPoint);
        }

        if (searchTimer >= currentSearchDuration)
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
            EnterSuspicious(heardPosition);
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
        currentSearchDuration = EffectiveInvestigateDuration;

        ChangeState(EnemyState.Investigate);
        SetDestination(targetPosition);
    }

    private void EnterChase()
    {
        bool wasAlreadyChasing = currentState == EnemyState.Chase;

        CancelChaseAudioFade();
        ChangeState(EnemyState.Chase);

        if (!wasAlreadyChasing)
        {
            if (useDungeonAlert && DungeonAlertSystem.Instance != null)
                DungeonAlertSystem.Instance.AddAlert(alertIncreaseOnDetection);

            if (detectAudio != null)
                detectAudio.Play();

            isInDetectionPause = true;
            detectionPauseTimer = detectionPauseDuration;
            agent.isStopped = true;

            StartChaseLoopWithDelay();
        }
        else
        {
            if (CanSeePlayer())
                SetDestination(GetPlayerPosition());
            else
                SetDestination(lastKnownPlayerPosition);
        }
    }

    private void EnterCatch()
    {
        isInDetectionPause = false;
        FadeOutChaseAudio();
        ChangeState(EnemyState.Catch);
    }

    private void EnterSearch(Vector3 targetPosition)
    {
        isInDetectionPause = false;
        FadeOutChaseAudio();
        onPlayerLost?.Invoke();

        lastKnownPlayerPosition = targetPosition;
        searchTimer = 0f;
        searchPointTimer = 0f;
        currentSearchDuration = EffectiveSearchDurationAfterChase;

        ChangeState(EnemyState.Search);
        SetDestination(targetPosition);
    }

    private void EnterReturnToPatrol()
    {
        isInDetectionPause = false;
        FadeOutChaseAudio();
        ChangeState(EnemyState.ReturnToPatrol);

        if (patrolPoints != null && patrolPoints.Length > 0)
            SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    private void ChangeState(EnemyState newState)
    {
        currentState = newState;
        SetSpeedForState();

        if (currentState != EnemyState.Catch && !isInDetectionPause)
            agent.isStopped = false;
    }

    private void SetSpeedForState()
    {
        float speedMultiplier = CurrentSpeedMultiplier;

        switch (currentState)
        {
            case EnemyState.Patrol:
            case EnemyState.ReturnToPatrol:
                agent.speed = patrolSpeed * speedMultiplier;
                break;

            case EnemyState.Suspicious:
            case EnemyState.Investigate:
                agent.speed = suspiciousSpeed * speedMultiplier;
                break;

            case EnemyState.Search:
                agent.speed = searchSpeed * speedMultiplier;
                break;

            case EnemyState.Chase:
            case EnemyState.Catch:
                agent.speed = chaseSpeed * speedMultiplier;
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

        if (isInDetectionPause)
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

    public void ForceChaseFromExternalAlert()
    {
        if (player == null || agent == null)
            return;

        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        lastKnownPlayerPosition = GetPlayerPosition();
        heardPosition = GetPlayerPosition();
        hasHeardSomething = true;
        detectionMeter = detectionThreshold;

        EnterChase();
    }

    private void CatchPlayer()
    {
        if (isHandlingCapture)
            return;

        FadeOutChaseAudio();
        onPlayerCaught?.Invoke();

        Debug.Log("Inimigo: Player capturado.");

        isHandlingCapture = true;
        respawnTimer = respawnDelay;
        agent.isStopped = true;
    }

    private void DoRespawn()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RespawnPlayerAtCheckpoint();
        }
        else
        {
            Debug.LogWarning("SaveManager não encontrado. Respawn não executado.");
        }

        ResetEnemyAfterRespawn();
    }

    private void ResetEnemyAfterRespawn()
    {
        detectionMeter = 0f;
        lostSightTimer = 0f;
        catchTimer = 0f;
        searchTimer = 0f;
        searchPointTimer = 0f;
        suspiciousTimer = 0f;
        patrolWaitTimer = 0f;
        detectionPauseTimer = 0f;
        currentSearchDuration = 0f;
        postRespawnIgnoreTimer = postRespawnIgnoreDuration;

        hasHeardSomething = false;
        hadPlayerInSightLastFrame = false;
        isInDetectionPause = false;

        StopAllChaseAudioCoroutinesAndSilence();

        if (agent.enabled && agent.isOnNavMesh)
            agent.ResetPath();

        ChangeState(EnemyState.ReturnToPatrol);

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            SetDestination(patrolPoints[currentPatrolIndex].position);
        }
        else
        {
            if (agent.enabled && agent.isOnNavMesh)
                agent.ResetPath();

            ChangeState(EnemyState.Patrol);
        }
    }

    private void StartChaseLoopWithDelay()
    {
        if (chaseLoopAudio == null)
            return;

        CancelChaseAudioFade();

        if (chaseLoopStartRoutine != null)
            StopCoroutine(chaseLoopStartRoutine);

        chaseLoopStartRoutine = StartCoroutine(DelayedStartChaseLoop());
    }

    private IEnumerator DelayedStartChaseLoop()
    {
        yield return new WaitForSeconds(chaseLoopStartDelay);

        if (currentState == EnemyState.Chase && chaseLoopAudio != null)
        {
            chaseLoopAudio.volume = chaseLoopOriginalVolume;

            if (!chaseLoopAudio.isPlaying)
                chaseLoopAudio.Play();
        }

        chaseLoopStartRoutine = null;
    }

    private void FadeOutChaseAudio()
    {
        if (chaseLoopAudio == null)
            return;

        if (chaseLoopStartRoutine != null)
        {
            StopCoroutine(chaseLoopStartRoutine);
            chaseLoopStartRoutine = null;
        }

        if (!chaseLoopAudio.isPlaying)
            return;

        CancelChaseAudioFade();
        chaseLoopFadeRoutine = StartCoroutine(FadeOutChaseLoopRoutine());
    }

    private IEnumerator FadeOutChaseLoopRoutine()
    {
        float startVolume = chaseLoopAudio.volume;
        float timer = 0f;

        while (timer < chaseLoopFadeOutDuration)
        {
            timer += Time.deltaTime;
            float t = timer / chaseLoopFadeOutDuration;
            chaseLoopAudio.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        chaseLoopAudio.Stop();
        chaseLoopAudio.volume = chaseLoopOriginalVolume;
        chaseLoopFadeRoutine = null;
    }

    private void CancelChaseAudioFade()
    {
        if (chaseLoopFadeRoutine != null)
        {
            StopCoroutine(chaseLoopFadeRoutine);
            chaseLoopFadeRoutine = null;
        }

        if (chaseLoopAudio != null)
            chaseLoopAudio.volume = chaseLoopOriginalVolume;
    }

    private void StopAllChaseAudioCoroutinesAndSilence()
    {
        if (chaseLoopStartRoutine != null)
        {
            StopCoroutine(chaseLoopStartRoutine);
            chaseLoopStartRoutine = null;
        }

        if (chaseLoopFadeRoutine != null)
        {
            StopCoroutine(chaseLoopFadeRoutine);
            chaseLoopFadeRoutine = null;
        }

        if (chaseLoopAudio != null)
        {
            chaseLoopAudio.Stop();
            chaseLoopAudio.volume = chaseLoopOriginalVolume;
        }
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