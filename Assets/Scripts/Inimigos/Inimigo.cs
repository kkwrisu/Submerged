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

    [Header("Hearing - Direct Chase")]
    [Tooltip("Nivel minimo de ruido (0-1) para acionar perseguicao direta ao ouvir o jogador")]
    public float directChaseNoiseThreshold = 0.65f;
    [Range(0f, 1f)]
    [Tooltip("Fracao do raio de audicao efetivo dentro da qual o ruido alto aciona perseguicao direta")]
    public float directChaseRadiusFraction = 0.4f;

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
    [Range(0f, 100f)] public float alertIncreaseOnDetection = 12.5f;

    [Header("Audio")]
    public AudioSource detectAudio;
    public AudioSource chaseLoopAudio;
    public AudioSource uppercutAudio;
    public float chaseLoopStartDelay = 0.4f;
    public float chaseLoopFadeOutDuration = 0.8f;

    [Header("Chase Music Resume")]
    public float chaseMusicResumeWindow = 8f;

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
    private bool isWaitingForUppercut;

    private Coroutine chaseLoopStartRoutine;
    private Coroutine chaseLoopFadeRoutine;
    private float chaseLoopOriginalVolume = 1f;

    private float savedChaseMusicTime = 0f;
    private float timeSinceChaseEnded = 0f;
    private bool chaseJustEnded = false;

    private Animator animator;

    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashChase = Animator.StringToHash("Chase");
    private static readonly int HashCatch = Animator.StringToHash("Catch");
    private static readonly int HashSuspicious = Animator.StringToHash("Suspicious");
    private static readonly int HashTurningL = Animator.StringToHash("TurningLeft");
    private static readonly int HashTurningR = Animator.StringToHash("TurningRight");
    private static readonly int HashLookout = Animator.StringToHash("Lookout");
    private static readonly int HashAnnoyed = Animator.StringToHash("Annoyed");

    private float idleTimer = 0f;
    private float idleGestureInterval = 8f;

    private PlayerMovement playerMovement;

    private float CurrentSpeedMultiplier
    {
        get
        {
            if (!useDungeonAlert || DungeonAlertSystem.Instance == null) return 1f;
            return DungeonAlertSystem.Instance.SpeedMultiplier;
        }
    }

    private float CurrentTimeMultiplier
    {
        get
        {
            if (!useDungeonAlert || DungeonAlertSystem.Instance == null) return 1f;
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

        animator = GetComponentInChildren<Animator>();
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

        if (player != null)
            playerMovement = player.GetComponent<PlayerMovement>();

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
        if (player == null || agent == null) return;
        if (!agent.enabled || !agent.isOnNavMesh) return;

        if (postRespawnIgnoreTimer > 0f)
            postRespawnIgnoreTimer -= Time.deltaTime;

        if (isHandlingCapture) return;

        if (chaseJustEnded)
        {
            timeSinceChaseEnded += Time.deltaTime;
            if (timeSinceChaseEnded >= chaseMusicResumeWindow)
            {
                chaseJustEnded = false;
                timeSinceChaseEnded = 0f;
                savedChaseMusicTime = 0f;
            }
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
                    if (CanSeePlayer()) SetDestination(GetPlayerPosition());
                    else SetDestination(lastKnownPlayerPosition);
                }
            }

            UpdateAnimator();
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
            case EnemyState.Patrol: UpdatePatrol(canSeePlayer, canHearPlayer); break;
            case EnemyState.Suspicious: UpdateSuspicious(canSeePlayer, canHearPlayer); break;
            case EnemyState.Investigate: UpdateInvestigate(canSeePlayer, canHearPlayer); break;
            case EnemyState.Chase: UpdateChase(canSeePlayer, canHearPlayer); break;
            case EnemyState.Catch: UpdateCatch(canSeePlayer); break;
            case EnemyState.Search: UpdateSearch(canSeePlayer, canHearPlayer); break;
            case EnemyState.ReturnToPatrol: UpdateReturnToPatrol(canSeePlayer, canHearPlayer); break;
        }

        hadPlayerInSightLastFrame = canSeePlayer;
        UpdateAnimator();
    }

    // ---------------------------------------------------------------------------
    // Determina se o ruido ouvido e forte e proximo o suficiente para acionar
    // perseguicao direta, sem passar por Suspicious/Investigate.
    // ---------------------------------------------------------------------------
    private bool ShouldChaseFromSound()
    {
        if (player == null || playerStealth == null) return false;

        float noise = playerStealth.CurrentNoise;
        if (noise < directChaseNoiseThreshold) return false;

        float distance = Vector3.Distance(transform.position, player.position);
        float effectiveRadius = hearingRadius * Mathf.Lerp(0.5f, 1.5f, noise);

        return distance <= effectiveRadius * directChaseRadiusFraction;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        float speed = agent.velocity.magnitude / chaseSpeed;
        animator.SetFloat(HashSpeed, speed, 0.1f, Time.deltaTime);

        if (speed > 0.1f)
        {
            float cross = Vector3.Cross(transform.forward, agent.velocity.normalized).y;
            float dot = Vector3.Dot(transform.forward, agent.velocity.normalized);
            animator.SetBool(HashTurningL, cross < -0.5f && dot < 0.5f);
            animator.SetBool(HashTurningR, cross > 0.5f && dot < 0.5f);
        }
        else
        {
            animator.SetBool(HashTurningL, false);
            animator.SetBool(HashTurningR, false);
        }

        bool isSearching = currentState == EnemyState.Search
                        || currentState == EnemyState.Investigate
                        || currentState == EnemyState.Suspicious;

        if (isSearching && speed < 0.05f)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleGestureInterval)
            {
                idleTimer = 0f;
                idleGestureInterval = Random.Range(3f, 7f);
                animator.SetTrigger(HashLookout);
            }
        }
        else if (currentState == EnemyState.Patrol && speed < 0.05f)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleGestureInterval)
            {
                idleTimer = 0f;
                idleGestureInterval = Random.Range(8f, 16f);
                animator.SetTrigger(HashAnnoyed);
            }
        }
        else
        {
            idleTimer = 0f;
            idleGestureInterval = Random.Range(8f, 16f);
        }
    }

    private void UpdatePatrol(bool canSeePlayer, bool canHearPlayer)
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;

        if (canSeePlayer && detectionMeter >= detectionThreshold) { EnterChase(); return; }

        if (canHearPlayer)
        {
            if (ShouldChaseFromSound())
            {
                detectionMeter = detectionThreshold;
                EnterChase();
            }
            else
            {
                EnterSuspicious(heardPosition);
            }
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0) return;

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
        if (!agent.enabled || !agent.isOnNavMesh) return;

        if (canSeePlayer && detectionMeter >= detectionThreshold) { EnterChase(); return; }

        // Ruido alto e proximo enquanto ja esta desconfiado: perseguicao imediata
        if (canHearPlayer && ShouldChaseFromSound())
        {
            detectionMeter = detectionThreshold;
            EnterChase();
            return;
        }

        suspiciousTimer += Time.deltaTime;
        if (canHearPlayer) suspiciousTimer = 0f;

        if (!agent.pathPending && agent.remainingDistance <= 1f)
        {
            EnterInvestigate(hasHeardSomething ? heardPosition : lastKnownPlayerPosition);
            return;
        }

        if (suspiciousTimer >= EffectiveInvestigateSoundDuration)
            EnterInvestigate(hasHeardSomething ? heardPosition : transform.position);
    }

    private void UpdateInvestigate(bool canSeePlayer, bool canHearPlayer)
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;

        if (canSeePlayer && detectionMeter >= detectionThreshold) { EnterChase(); return; }

        // Ruido alto e proximo enquanto investiga: perseguicao imediata
        if (canHearPlayer && ShouldChaseFromSound())
        {
            detectionMeter = detectionThreshold;
            EnterChase();
            return;
        }

        searchTimer += Time.deltaTime;
        searchPointTimer += Time.deltaTime;

        if (searchPointTimer >= EffectiveSearchPointInterval)
        {
            searchPointTimer = 0f;
            SetDestination(GetRandomPointNear(lastKnownPlayerPosition, searchRadius));
        }

        if (searchTimer >= currentSearchDuration)
            EnterReturnToPatrol();
    }

    private void UpdateChase(bool canSeePlayer, bool canHearPlayer)
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;

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

        if (Vector3.Distance(transform.position, GetPlayerPosition()) <= catchRange)
            EnterCatch();
    }

    private void UpdateCatch(bool canSeePlayer)
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;

        agent.isStopped = true;

        if (!isWaitingForUppercut)
        {
            Vector3 lookTarget = GetPlayerPosition();
            lookTarget.y = transform.position.y;
            Vector3 dir = (lookTarget - transform.position).normalized;

            if (dir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            }
        }

        float distanceToPlayer = Vector3.Distance(transform.position, GetPlayerPosition());

        if (distanceToPlayer > catchRange + 0.4f)
        {
            agent.isStopped = false;
            EnterChase();
            return;
        }

        if (catchTimer <= 0f && !isWaitingForUppercut)
            StartCoroutine(WaitForUppercutAndCatch());

        if (!canSeePlayer && distanceToPlayer > catchRange)
        {
            agent.isStopped = false;
            EnterSearch(lastKnownPlayerPosition);
        }
    }

    private void UpdateSearch(bool canSeePlayer, bool canHearPlayer)
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;

        if (canSeePlayer && detectionMeter >= detectionThreshold) { EnterChase(); return; }

        if (canHearPlayer)
        {
            if (ShouldChaseFromSound())
            {
                detectionMeter = detectionThreshold;
                EnterChase();
            }
            else
            {
                EnterSuspicious(heardPosition);
            }
            return;
        }

        searchTimer += Time.deltaTime;
        searchPointTimer += Time.deltaTime;

        if (searchPointTimer >= EffectiveSearchPointInterval)
        {
            searchPointTimer = 0f;
            SetDestination(GetRandomPointNear(lastKnownPlayerPosition, searchRadius));
        }

        if (searchTimer >= currentSearchDuration)
            EnterReturnToPatrol();
    }

    private void UpdateReturnToPatrol(bool canSeePlayer, bool canHearPlayer)
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;

        if (canSeePlayer && detectionMeter >= detectionThreshold) { EnterChase(); return; }

        if (canHearPlayer)
        {
            if (ShouldChaseFromSound())
            {
                detectionMeter = detectionThreshold;
                EnterChase();
            }
            else
            {
                EnterSuspicious(heardPosition);
            }
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= patrolPointReachDistance)
        {
            ChangeState(EnemyState.Patrol);
            GoToCurrentPatrolPoint();
        }
    }

    private void EnterSuspicious(Vector3 targetPosition)
    {
        hasHeardSomething = true;
        heardPosition = targetPosition;
        suspiciousTimer = 0f;
        ChangeState(EnemyState.Suspicious);
        SetDestination(targetPosition);
        animator?.SetTrigger(HashSuspicious);
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
            Debug.Log("Inimigo entrou em CHASE");

            if (useDungeonAlert && DungeonAlertSystem.Instance != null)
                DungeonAlertSystem.Instance.AddAlert(alertIncreaseOnDetection);
            else
                Debug.LogWarning("DungeonAlertSystem nao encontrado ou desativado!");

            if (detectAudio != null)
                detectAudio.Play();

            isInDetectionPause = true;
            detectionPauseTimer = detectionPauseDuration;
            agent.isStopped = true;

            animator?.SetTrigger(HashChase);
            StartChaseLoopWithDelay();
        }
        else
        {
            if (CanSeePlayer()) SetDestination(GetPlayerPosition());
            else SetDestination(lastKnownPlayerPosition);
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

        if (chaseLoopAudio != null && chaseLoopAudio.isPlaying)
        {
            savedChaseMusicTime = chaseLoopAudio.time;
            chaseJustEnded = true;
            timeSinceChaseEnded = 0f;
        }

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

    private IEnumerator WaitForUppercutAndCatch()
    {
        isWaitingForUppercut = true;
        agent.isStopped = true;

        Vector3 lookTarget = GetPlayerPosition();
        lookTarget.y = transform.position.y;
        Vector3 lookDir = (lookTarget - transform.position).normalized;
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(lookDir);

        animator?.SetTrigger(HashCatch);

        yield return null;
        yield return null;

        float clipLength = animator != null
            ? animator.GetCurrentAnimatorStateInfo(0).length
            : 1f;

        yield return new WaitForSeconds(clipLength * 0.70f);

        if (uppercutAudio != null) uppercutAudio.Play();

        if (playerMovement == null && player != null)
            playerMovement = player.GetComponent<PlayerMovement>();

        playerMovement?.LockForCapture();
        onPlayerCaught?.Invoke();

        PlayerLook playerLook = player?.GetComponentInChildren<PlayerLook>();
        float fallDuration = playerLook != null ? playerLook.knockbackDuration : 1.4f;

        // Dispara a animacao de camera (queda pra tras) no momento do golpe.
        // Antes so liamos knockbackDuration pra saber quanto esperar, mas
        // nunca chamavamos o metodo que realmente inicia o efeito.
        playerLook?.TriggerKnockback();

        yield return new WaitForSeconds(fallDuration);

        CatchPlayer();
        isWaitingForUppercut = false;
    }

    private void CatchPlayer()
    {
        if (isHandlingCapture) return;

        FadeOutChaseAudio();
        Debug.Log("Inimigo: Player capturado.");

        isHandlingCapture = true;
        agent.isStopped = true;
        postRespawnIgnoreTimer = postRespawnIgnoreDuration;

        if (CaptureHandler.Instance != null)
            CaptureHandler.Instance.HandleCapture();
        else
            DoRespawn();

        StartCoroutine(ResetAfterDelay());
    }

    private IEnumerator ResetAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        isHandlingCapture = false;
        ResetEnemyAfterRespawn();
    }

    private void DoRespawn()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.RespawnPlayerAtCheckpoint();
        else
            Debug.LogWarning("SaveManager nao encontrado. Respawn nao executado.");

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
        idleTimer = 0f;
        idleGestureInterval = Random.Range(8f, 16f);
        isWaitingForUppercut = false;

        hasHeardSomething = false;
        hadPlayerInSightLastFrame = false;
        isInDetectionPause = false;

        savedChaseMusicTime = 0f;
        chaseJustEnded = false;
        timeSinceChaseEnded = 0f;

        StopAllChaseAudioCoroutinesAndSilence();

        playerMovement?.UnlockFromCapture();

        if (agent.enabled && agent.isOnNavMesh)
            agent.ResetPath();

        ChangeState(EnemyState.ReturnToPatrol);

        if (patrolPoints != null && patrolPoints.Length > 0)
            SetDestination(patrolPoints[currentPatrolIndex].position);
        else
        {
            if (agent.enabled && agent.isOnNavMesh)
                agent.ResetPath();
            ChangeState(EnemyState.Patrol);
        }
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
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    private void SetDestination(Vector3 position)
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;
        if (isInDetectionPause) return;

        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            position = hit.position;

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
            Vector3 random = center + new Vector3(
                Random.Range(-radius, radius), 0f, Random.Range(-radius, radius));

            if (NavMesh.SamplePosition(random, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                return hit.position;
        }

        return center;
    }

    private void UpdateDetectionMeter(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            float multiplier = 1f;

            if (playerStealth != null)
            {
                if (playerStealth.isCrouching) multiplier *= 0.7f;
                if (playerStealth.isSprinting) multiplier *= 1.35f;
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
        if (player == null) return false;

        Vector3 origin = eyePoint != null ? eyePoint.position : transform.position + Vector3.up * 1.6f;
        Vector3 target = player.position + Vector3.up * 1.0f;
        Vector3 toPlayer = target - origin;

        float distance = toPlayer.magnitude;
        float adjustedViewDistance = viewDistance;

        if (currentState == EnemyState.Suspicious || currentState == EnemyState.Investigate)
            adjustedViewDistance *= suspiciousViewMultiplier;

        if (playerStealth != null)
        {
            if (playerStealth.isCrouching) adjustedViewDistance *= crouchVisionMultiplier;
            if (playerStealth.isSprinting) adjustedViewDistance *= sprintVisionMultiplier;
        }

        if (distance > adjustedViewDistance) return false;

        Vector3 effectiveForward;
        if (agent != null && agent.velocity.sqrMagnitude > 0.1f)
        {
            Vector3 agentDir = agent.velocity.normalized;
            agentDir.y = 0f;
            effectiveForward = Vector3.Slerp(transform.forward, agentDir, 0.6f).normalized;
        }
        else
        {
            effectiveForward = transform.forward;
        }

        float alertedAngleBonus = 0f;
        if (currentState == EnemyState.Suspicious
         || currentState == EnemyState.Investigate
         || currentState == EnemyState.Search)
        {
            alertedAngleBonus = Mathf.Lerp(40f, 10f, Mathf.Clamp01(distance / viewDistance));
        }

        float angle = Vector3.Angle(effectiveForward, toPlayer.normalized);
        if (angle > (viewAngle * 0.5f) + alertedAngleBonus) return false;

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
        if (player == null || playerStealth == null) return false;

        float noise = playerStealth.CurrentNoise;
        if (noise < hearingThreshold) return false;

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

    public void ForceChaseFromExternalAlert(bool skipAlertIncrease = false)
    {
        if (player == null || agent == null) return;
        if (!agent.enabled || !agent.isOnNavMesh) return;

        lastKnownPlayerPosition = GetPlayerPosition();
        heardPosition = GetPlayerPosition();
        hasHeardSomething = true;
        detectionMeter = detectionThreshold;

        if (skipAlertIncrease)
        {
            bool wasAlreadyChasing = currentState == EnemyState.Chase;
            ChangeState(EnemyState.Chase);

            if (!wasAlreadyChasing)
            {
                if (detectAudio != null)
                    detectAudio.Play();

                isInDetectionPause = true;
                detectionPauseTimer = detectionPauseDuration;
                agent.isStopped = true;

                animator?.SetTrigger(HashChase);
                StartChaseLoopWithDelay();
            }
            else
            {
                SetDestination(GetPlayerPosition());
            }
        }
        else
        {
            EnterChase();
        }
    }

    private void StartChaseLoopWithDelay()
    {
        if (chaseLoopAudio == null) return;

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
            {
                if (chaseJustEnded && timeSinceChaseEnded < chaseMusicResumeWindow && savedChaseMusicTime > 0f)
                    chaseLoopAudio.time = savedChaseMusicTime;
                else
                    chaseLoopAudio.time = 0f;

                chaseLoopAudio.Play();

                chaseJustEnded = false;
                timeSinceChaseEnded = 0f;
            }
        }

        chaseLoopStartRoutine = null;
    }

    private void FadeOutChaseAudio()
    {
        if (chaseLoopAudio == null) return;

        if (chaseLoopStartRoutine != null)
        {
            StopCoroutine(chaseLoopStartRoutine);
            chaseLoopStartRoutine = null;
        }

        if (!chaseLoopAudio.isPlaying) return;

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
            chaseLoopAudio.volume = Mathf.Lerp(startVolume, 0f, timer / chaseLoopFadeOutDuration);
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

        // Visualiza o raio de perseguicao direta por audio (fracao do raio de audicao)
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, hearingRadius * directChaseRadiusFraction);

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