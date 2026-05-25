using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;
    private Vector2 moveInput;

    [Header("References")]
    public Player_Status stealthStatus;

    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2.5f;

    public float acceleration = 10f;
    public float deceleration = 12f;
    public float airControl = 0.5f;

    private Vector3 currentVelocity;

    [Header("Jump / Gravity")]
    public float gravity = -25f;
    public float fallMultiplier = 2.2f;
    public float lowJumpMultiplier = 2f;
    public float jumpHeight = 1.6f;

    private float yVelocity;

    private bool sprintHeld;
    private bool isSprinting;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;
    private bool isGrounded;
    private bool wasGroundedLastFrame;

    [Header("Step Climb")]
    public Transform lowerStepCheck;
    public Transform upperStepCheck;
    public float stepCheckDistance = 0.6f;
    public float stepHeight = 0.18f;
    public float stepCooldown = 0.2f;

    private float stepTimer;

    [Header("Ledge / Climb")]
    public Transform ledgeCheck;
    public float ledgeCheckDistance = 0.6f;
    public float ledgeHeight = 1.5f;
    public float climbSpeed = 3f;

    public float pullUpForward = 0.6f;
    public float pullUpUpward = 0.4f;
    public float pullUpSpeed = 6f;

    public float hangInputDelay = 0.2f;
    public float hangDropSpeed = 8f;

    [Header("Fixed Hang Position")]
    public float hangBackOffset = 0.35f;

    [Tooltip("Quanto a cabeça do player fica acima da borda.")]
    public float hangHeadAboveLedge = 0.15f;

    [Tooltip("Velocidade do encaixe horizontal ao agarrar a borda.")]
    public float hangHorizontalSnapSpeed = 14f;

    [Header("Ledge Blocking Near Ladder")]
    public float ledgeBlockAfterLadderTime = 0.25f;

    [Header("Ledge Release")]
    public float ledgeBlockAfterReleaseTime = 0.25f;

    [Header("Climb Timeout")]
    [Tooltip("Tempo máximo em cada fase do climb antes de cancelar automaticamente.")]
    public float climbTimeout = 1.5f;

    private float hangTimeout;
    private float climbingTimeout;
    private float pullUpTimeout;

    private float hangTimer;
    private bool isDroppingToHang;
    private float ledgeBlockTimer;

    private bool isClimbing;
    private bool isHanging;
    private bool isPullingUp;

    private bool ledgeGrabThisFrame;

    private Vector3 climbTarget;
    private Vector3 hangTargetPosition;
    private float climbStartY;
    private Vector3 pullUpTarget;

    // Quando true, ao terminar o pull-up o player agacha automaticamente
    private bool pullUpForceCrouch;

    [Header("Crouch")]
    public float crouchHeight = 1f;
    public float crouchTransitionSpeed = 12f;

    private bool crouchHeld;
    private bool isCrouching;

    private float standingHeight;
    private Vector3 standingCenter;
    private Vector3 crouchingCenter;

    [Header("Slide")]
    public float slideInitialSpeed = 10f;
    public float slideDeceleration = 8f;
    public float slideMinSpeed = 1.5f;
    public float slideCooldown = 1f;
    public float slideShiftGraceTime = 0.3f;

    [Tooltip("Impulso horizontal preservado ao pular durante o slide (0 = nenhum, 1 = total).")]
    [Range(0f, 1f)]
    public float slideJumpMomentumRetain = 0.8f;

    private bool isSliding;
    private Vector3 slideDirection;
    private float slideSpeed;
    private float slideCooldownTimer;
    private float shiftGraceTimer;

    [Header("Wall Jump")]
    public float wallJumpUpForce = 7f;
    public float wallJumpAwayForce = 5f;
    public float wallCheckDistance = 0.65f;
    public float wallJumpCooldown = 0.15f;
    public LayerMask wallMask;

    [Header("Wall Slide")]
    [Tooltip("Velocidade de queda enquanto deslizando na parede.")]
    public float wallSlideSpeed = 1.8f;
    [Tooltip("Quão rápido o player desacelera até o wallSlideSpeed ao encostar na parede.")]
    public float wallSlideGravityDamp = 12f;

    [Tooltip("Dot product mínimo entre a normal atual e a última parede para considerar paredes DIFERENTES.")]
    public float wallNormalDifferenceThreshold = 0.3f;

    private float wallJumpCooldownTimer;
    private Vector3 lastWallNormal = Vector3.zero;
    private bool isWallSliding;
    private Vector3 currentWallNormal;

    [Header("Ladder")]
    public string ladderTag = "Ladder";
    public float ladderEnterSpeedThreshold = 0.1f;
    public float ladderDetachJumpForce = 5f;
    public float ladderHorizontalExitForce = 2f;

    [Header("Ladder Side Exit")]
    public float ladderSideExitThreshold = 0.3f;
    public float ladderReenterBlockTime = 0.2f;

    [Header("Ladder Speeds")]
    public float ladderClimbUpSpeed = 3.5f;
    public float ladderClimbDownSpeed = 3.5f;
    public float ladderFastDownSpeed = 7f;

    [Tooltip("Se estiver parado na escada, fica totalmente parado.")]
    public bool ladderStayStillWhenIdle = true;

    [Header("Ladder Top Exit")]
    [Tooltip("Empurrão pra frente ao sair pelo topo.")]
    public float ladderTopExitForwardOffset = 0.55f;

    [Tooltip("Altura extra ao sair pelo topo.")]
    public float ladderTopExitUpOffset = 0.15f;

    [Tooltip("Velocidade de encaixe ao subir por cima da escada.")]
    public float ladderTopExitMoveSpeed = 6f;

    [Tooltip("Bloqueio de reentrada após sair pelo topo. Mantenha >= 0.5 para evitar bugs.")]
    public float ladderTopExitReenterBlockTime = 0.5f;

    private float ladderReenterTimer;

    private Ladder currentLadder;
    private bool isOnLadder;
    private bool isInsideLadder;

    private bool isExitingLadderTop;
    private Vector3 ladderTopExitTarget;

    // -------------------------------------------------------------------------
    // Capture lock
    // -------------------------------------------------------------------------
    private bool _capturedLock = false;

    public void LockForCapture()
    {
        _capturedLock = true;
        moveInput = Vector2.zero;
        currentVelocity = Vector3.zero;
        yVelocity = -2f;
        sprintHeld = false;
        isSprinting = false;
    }

    public void UnlockFromCapture()
    {
        _capturedLock = false;
    }

    // -------------------------------------------------------------------------

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (stealthStatus == null)
            stealthStatus = GetComponent<Player_Status>();

        standingHeight = controller.height;
        standingCenter = controller.center;

        float heightDifference = standingHeight - crouchHeight;
        crouchingCenter = standingCenter - Vector3.up * (heightDifference * 0.5f);
    }

    void Update()
    {
        if (_capturedLock) return;

        stepTimer -= Time.deltaTime;
        hangTimer -= Time.deltaTime;
        ledgeBlockTimer -= Time.deltaTime;
        ladderReenterTimer -= Time.deltaTime;
        slideCooldownTimer -= Time.deltaTime;
        wallJumpCooldownTimer -= Time.deltaTime;
        shiftGraceTimer -= Time.deltaTime;

        HandleGroundCheck();
        HandleCrouch();
        UpdateSprintState();

        if (isExitingLadderTop)
        {
            HandleLadderTopExit();
            UpdateStealthStatus();
            return;
        }

        if (isOnLadder)
        {
            HandleLadderMovement();
            UpdateStealthStatus();
            return;
        }

        TryEnterLadder();

        ledgeGrabThisFrame = false;

        if (CanCheckLedge())
            CheckLedge();

        HandleHang();
        HandleClimb();
        HandlePullUp();
        HandleSlide();
        HandleWallSlide();
        HandleMovement();

        UpdateStealthStatus();
    }

    void UpdateSprintState()
    {
        bool canSprint =
            sprintHeld &&
            !isCrouching &&
            !isOnLadder &&
            !isExitingLadderTop &&
            !isClimbing &&
            !isHanging &&
            !isDroppingToHang &&
            !isPullingUp &&
            !isSliding &&
            moveInput.magnitude > 0.1f;

        isSprinting = canSprint;
    }

    bool CanCheckLedge()
    {
        if (isGrounded) return false;
        if (isExitingLadderTop) return false;
        if (isClimbing || isHanging || isDroppingToHang || isPullingUp) return false;
        if (isOnLadder) return false;
        if (isInsideLadder) return false;
        if (currentLadder != null) return false;
        if (ledgeBlockTimer > 0f) return false;

        return true;
    }

    void HandleGroundCheck()
    {
        wasGroundedLastFrame = isGrounded;

        if (isOnLadder || isExitingLadderTop)
        {
            isGrounded = false;
            return;
        }

        Vector3 capsuleStart = groundCheck.position + Vector3.up * 0.1f;
        Vector3 capsuleEnd = groundCheck.position;

        isGrounded = Physics.CheckCapsule(
            capsuleStart,
            capsuleEnd,
            groundDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (isGrounded && yVelocity < 0)
            yVelocity = -2f;

        if (isGrounded)
        {
            lastWallNormal = Vector3.zero;
            isWallSliding = false;
        }

        if (!wasGroundedLastFrame && isGrounded && stealthStatus != null)
            stealthStatus.TriggerLandingNoise();
    }

    void HandleCrouch()
    {
        if (isSliding)
        {
            float oldHeight = controller.height;

            controller.height = Mathf.Lerp(controller.height, crouchHeight, crouchTransitionSpeed * Time.deltaTime);
            controller.center = Vector3.Lerp(controller.center, crouchingCenter, crouchTransitionSpeed * Time.deltaTime);

            float heightDelta = controller.height - oldHeight;
            if (Mathf.Abs(heightDelta) > 0.0001f)
                controller.Move(Vector3.up * (heightDelta * 0.5f));

            return;
        }

        bool canCrouchNow = !isClimbing && !isHanging && !isDroppingToHang && !isPullingUp && !isOnLadder && !isExitingLadderTop;

        if (crouchHeld && canCrouchNow)
        {
            isCrouching = true;
        }
        else
        {
            if (CanStandUp())
                isCrouching = false;
            else
                isCrouching = true;
        }

        float oldH = controller.height;

        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        Vector3 targetCenter = isCrouching ? crouchingCenter : standingCenter;

        controller.height = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        controller.center = Vector3.Lerp(controller.center, targetCenter, crouchTransitionSpeed * Time.deltaTime);

        float heightDeltaNormal = controller.height - oldH;
        if (Mathf.Abs(heightDeltaNormal) > 0.0001f)
            controller.Move(Vector3.up * (heightDeltaNormal * 0.5f));

        if (isCrouching)
            isSprinting = false;
    }

    bool CanStandUp()
    {
        if (isClimbing || isHanging || isDroppingToHang || isPullingUp || isOnLadder || isExitingLadderTop)
            return true;

        float radius = controller.radius * 0.95f;
        Vector3 worldCenter = transform.position + standingCenter;

        float halfHeight = Mathf.Max(standingHeight * 0.5f, radius);
        Vector3 point1 = worldCenter + Vector3.up * (halfHeight - radius);
        Vector3 point2 = worldCenter - Vector3.up * (halfHeight - radius);

        return !Physics.CheckCapsule(
            point1,
            point2,
            radius,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }

    void StartSlide()
    {
        isSliding = true;
        isCrouching = true;
        crouchHeld = true;
        isSprinting = false;

        slideDirection = currentVelocity.magnitude > 0.1f
            ? currentVelocity.normalized
            : transform.forward;

        slideDirection.y = 0f;
        slideDirection.Normalize();

        slideSpeed = slideInitialSpeed;
        slideCooldownTimer = slideCooldown;
    }

    void StopSlide()
    {
        isSliding = false;
        crouchHeld = false;
    }

    void HandleSlide()
    {
        if (!isSliding) return;

        if (!isGrounded || isClimbing || isHanging || isDroppingToHang ||
            isPullingUp || isOnLadder || isExitingLadderTop)
        {
            StopSlide();
            return;
        }

        slideSpeed = Mathf.MoveTowards(slideSpeed, 0f, slideDeceleration * Time.deltaTime);

        if (slideSpeed <= slideMinSpeed)
        {
            StopSlide();
            return;
        }

        currentVelocity = slideDirection * slideSpeed;
        controller.Move(currentVelocity * Time.deltaTime);

        if (yVelocity < 0)
            yVelocity += gravity * fallMultiplier * Time.deltaTime;
        else
            yVelocity += gravity * Time.deltaTime;

        controller.Move(Vector3.up * yVelocity * Time.deltaTime);
    }

    void DoSlideJump()
    {
        currentVelocity = slideDirection * (slideSpeed * slideJumpMomentumRetain);
        yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        StopSlide();

        if (stealthStatus != null)
            stealthStatus.TriggerJumpNoise();

        FootstepAudio footsteps =
            GetComponent<FootstepAudio>();

        if (footsteps != null)
            footsteps.PlayJumpSound();
    }

    bool DetectWall(out Vector3 normal)
    {
        normal = Vector3.zero;

        Vector3[] directions = new Vector3[]
        {
            transform.forward,
            -transform.forward,
            transform.right,
            -transform.right
        };

        foreach (Vector3 dir in directions)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, dir, out hit, wallCheckDistance, wallMask, QueryTriggerInteraction.Ignore))
            {
                normal = hit.normal;
                return true;
            }
        }

        return false;
    }

    void HandleWallSlide()
    {
        if (isGrounded || isCrouching || isClimbing || isHanging || isDroppingToHang ||
            isPullingUp || isOnLadder || isExitingLadderTop || isSliding || ledgeGrabThisFrame)
        {
            isWallSliding = false;
            return;
        }

        Vector3 normal;
        bool wallFound = DetectWall(out normal);

        if (!wallFound)
        {
            isWallSliding = false;
            return;
        }

        bool wallIsNew = lastWallNormal == Vector3.zero ||
                         Vector3.Dot(normal, lastWallNormal) < wallNormalDifferenceThreshold;

        if (!wallIsNew)
        {
            isWallSliding = false;
            return;
        }

        isWallSliding = true;
        currentWallNormal = normal;
    }

    void DoWallJump()
    {
        lastWallNormal = currentWallNormal;
        wallJumpCooldownTimer = wallJumpCooldown;
        isWallSliding = false;

        currentVelocity = currentWallNormal * wallJumpAwayForce;
        yVelocity = Mathf.Sqrt(wallJumpUpForce * -2f * gravity);

        if (stealthStatus != null)
            stealthStatus.TriggerJumpNoise();

        FootstepAudio footsteps =
            GetComponent<FootstepAudio>();

        if (footsteps != null)
            footsteps.PlayJumpSound();
    }

    void HandleMovement()
    {
        if (isClimbing || isHanging || isDroppingToHang || isPullingUp || isOnLadder || isExitingLadderTop || isSliding) return;

        float targetSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);

        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        Vector3 targetVelocity = moveDirection * targetSpeed;

        float control = isGrounded ? 1f : airControl;
        float lerpSpeed = moveInput.magnitude > 0.01f ? acceleration : deceleration;

        currentVelocity = Vector3.Lerp(
            currentVelocity,
            targetVelocity,
            lerpSpeed * control * Time.deltaTime
        );

        StepClimb(moveDirection);

        controller.Move(currentVelocity * Time.deltaTime);

        if (isWallSliding)
        {
            yVelocity = Mathf.MoveTowards(yVelocity, -wallSlideSpeed, wallSlideGravityDamp * Time.deltaTime);
        }
        else if (yVelocity < 0)
        {
            yVelocity += gravity * fallMultiplier * Time.deltaTime;
        }
        else if (yVelocity > 0)
        {
            yVelocity += gravity * lowJumpMultiplier * Time.deltaTime;
        }
        else
        {
            yVelocity += gravity * Time.deltaTime;
        }

        controller.Move(Vector3.up * yVelocity * Time.deltaTime);
    }

    void StepClimb(Vector3 moveDirection)
    {
        if (!isGrounded || isCrouching || moveInput.magnitude < 0.1f || stepTimer > 0 || isOnLadder || isExitingLadderTop)
            return;

        RaycastHit lowerHit;

        if (Physics.Raycast(lowerStepCheck.position, moveDirection, out lowerHit, stepCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (!Physics.Raycast(upperStepCheck.position, moveDirection, stepCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                controller.Move(Vector3.up * stepHeight);
                stepTimer = stepCooldown;
            }
        }
    }

    void CheckLedge()
    {
        if (isOnLadder || isExitingLadderTop) return;
        if (isInsideLadder || currentLadder != null || ledgeBlockTimer > 0f)
            return;

        RaycastHit wallHit;

        if (Physics.Raycast(ledgeCheck.position, transform.forward, out wallHit, ledgeCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 origin = wallHit.point + Vector3.up * ledgeHeight;
            RaycastHit topHit;

            if (Physics.Raycast(origin, Vector3.down, out topHit, ledgeHeight, groundMask, QueryTriggerInteraction.Ignore))
            {
                float heightDifference = topHit.point.y - transform.position.y;

                if (heightDifference <= 0 || heightDifference > ledgeHeight)
                    return;

                if (Vector3.Distance(topHit.point, wallHit.point) > ledgeHeight + 0.3f)
                    return;

                if (Physics.CheckSphere(topHit.point + Vector3.up * 0.3f, 0.2f, groundMask, QueryTriggerInteraction.Ignore))
                    return;

                StartHang(topHit.point);
            }
        }
    }

    Vector3 CalculateHangPosition(Vector3 ledgePoint)
    {
        Vector3 targetPosition = ledgePoint - transform.forward * hangBackOffset;

        float targetHeadY = ledgePoint.y + hangHeadAboveLedge;
        float headOffsetFromPivot = standingCenter.y + (standingHeight * 0.5f);

        targetPosition.y = targetHeadY - headOffsetFromPivot;

        return targetPosition;
    }

    void StartHang(Vector3 ledgePoint)
    {
        if (isOnLadder) return;
        if (isExitingLadderTop) return;
        if (isInsideLadder) return;
        if (currentLadder != null) return;
        if (ledgeBlockTimer > 0f) return;

        climbTarget = ledgePoint;
        hangTargetPosition = CalculateHangPosition(ledgePoint);

        yVelocity = 0f;
        currentVelocity = Vector3.zero;

        hangTimer = hangInputDelay;
        hangTimeout = climbTimeout;
        isDroppingToHang = true;
        isHanging = false;
        isSprinting = false;

        ledgeGrabThisFrame = true;
    }

    void HandleHang()
    {
        if (isDroppingToHang)
        {
            float newX = Mathf.MoveTowards(transform.position.x, hangTargetPosition.x, hangHorizontalSnapSpeed * Time.deltaTime);
            float newZ = Mathf.MoveTowards(transform.position.z, hangTargetPosition.z, hangHorizontalSnapSpeed * Time.deltaTime);
            float newY = Mathf.MoveTowards(transform.position.y, hangTargetPosition.y, hangDropSpeed * Time.deltaTime);

            controller.enabled = false;
            transform.position = new Vector3(newX, newY, newZ);
            controller.enabled = true;

            bool reachedX = Mathf.Abs(transform.position.x - hangTargetPosition.x) <= 0.02f;
            bool reachedY = Mathf.Abs(transform.position.y - hangTargetPosition.y) <= 0.02f;
            bool reachedZ = Mathf.Abs(transform.position.z - hangTargetPosition.z) <= 0.02f;

            if (reachedX && reachedY && reachedZ)
            {
                controller.enabled = false;
                transform.position = hangTargetPosition;
                controller.enabled = true;

                isDroppingToHang = false;
                isHanging = true;
                hangTimeout = climbTimeout;
            }

            return;
        }

        if (!isHanging) return;

        controller.enabled = false;
        transform.position = hangTargetPosition;
        controller.enabled = true;

        if (moveInput.y < -0.1f)
        {
            isHanging = false;
            ledgeBlockTimer = ledgeBlockAfterReleaseTime;
            yVelocity = -5f;
            return;
        }

        if (hangTimer <= 0f && moveInput.y > 0.1f)
        {
            isHanging = false;
            StartClimb(climbTarget);
            return;
        }
    }

    void StartClimb(Vector3 ledgePoint)
    {
        if (isOnLadder) return;

        isClimbing = true;
        climbTarget = ledgePoint;
        climbStartY = transform.position.y;
        climbingTimeout = climbTimeout;
        yVelocity = 0f;
        isSprinting = false;
    }

    void HandleClimb()
    {
        if (!isClimbing) return;

        climbingTimeout -= Time.deltaTime;

        if (climbingTimeout <= 0f)
        {
            Debug.Log("[Climb] Timeout no isClimbing — cancelando.");
            CancelClimb();
            return;
        }

        Vector3 direction = (climbTarget - transform.position).normalized;

        float progress = Mathf.Clamp01((transform.position.y - climbStartY) / ledgeHeight);
        float curve = Mathf.Sin(progress * Mathf.PI);
        float verticalSpeed = climbSpeed * (0.6f + curve * 1.4f);

        controller.Move(new Vector3(direction.x, 0f, direction.z) * climbSpeed * 0.6f * Time.deltaTime);
        controller.Move(Vector3.up * verticalSpeed * Time.deltaTime);

        yVelocity = 0f;

        if (progress >= 1f)
        {
            isClimbing = false;
            StartPullUp();
        }
    }

    void StartPullUp()
    {
        isPullingUp = true;
        pullUpTimeout = climbTimeout;
        pullUpTarget = transform.position + transform.forward * pullUpForward + Vector3.up * pullUpUpward;
        isSprinting = false;

        // Verifica se não cabe em pé no destino — se sim, vai agachar ao terminar
        float radius = controller.radius * 0.9f;
        Vector3 standCenter = pullUpTarget + standingCenter;
        float halfStand = Mathf.Max(standingHeight * 0.5f, radius);
        Vector3 standP1 = standCenter + Vector3.up * (halfStand - radius);
        Vector3 standP2 = standCenter - Vector3.up * (halfStand - radius);
        bool canStand = !Physics.CheckCapsule(standP1, standP2, radius, groundMask, QueryTriggerInteraction.Ignore);

        pullUpForceCrouch = !canStand;
    }

    void HandlePullUp()
    {
        if (!isPullingUp) return;

        pullUpTimeout -= Time.deltaTime;

        if (pullUpTimeout <= 0f)
        {
            Debug.Log("[Climb] Timeout no isPullingUp — cancelando.");
            CancelClimb();
            return;
        }

        Vector3 toTarget = pullUpTarget - transform.position;
        controller.Move(toTarget.normalized * pullUpSpeed * Time.deltaTime);

        if (toTarget.magnitude < 0.05f)
        {
            isPullingUp = false;
            yVelocity = -2f;

            if (pullUpForceCrouch)
            {
                isCrouching = true;
                crouchHeld = true;
                pullUpForceCrouch = false;
            }
        }
    }

    private void CancelClimb()
    {
        isDroppingToHang = false;
        isHanging = false;
        isClimbing = false;
        isPullingUp = false;
        pullUpForceCrouch = false;
        ledgeGrabThisFrame = false;
        ledgeBlockTimer = ledgeBlockAfterReleaseTime;

        yVelocity = -3f;
        currentVelocity = Vector3.zero;

        controller.enabled = true;

        Debug.Log("[Climb] Cancelado — controle devolvido ao player.");
    }

    void TryEnterLadder()
    {
        if (ladderReenterTimer > 0f) return;
        if (!isInsideLadder || currentLadder == null) return;
        if (isClimbing || isHanging || isDroppingToHang || isPullingUp || isExitingLadderTop) return;
        if (isCrouching) return;

        if (Mathf.Abs(moveInput.y) > ladderEnterSpeedThreshold)
        {
            EnterLadder(currentLadder);
        }
    }

    void EnterLadder(Ladder ladder)
    {
        isOnLadder = true;
        isSprinting = false;
        isCrouching = false;

        currentVelocity = Vector3.zero;
        yVelocity = 0f;

        ledgeBlockTimer = ledgeBlockAfterLadderTime;
    }

    void ExitLadder(bool jumpedOff = false, bool exitedTop = false)
    {
        Ladder ladderBeforeExit = currentLadder;

        if (exitedTop && ladderBeforeExit != null)
        {
            StartLadderTopExit(ladderBeforeExit);
            return;
        }

        isOnLadder = false;
        isSprinting = false;
        ledgeBlockTimer = ledgeBlockAfterLadderTime;
        ladderReenterTimer = ladderReenterBlockTime;

        if (jumpedOff)
        {
            yVelocity = ladderDetachJumpForce;
            currentVelocity = transform.forward * ladderHorizontalExitForce;
        }
        else
        {
            yVelocity = -2f;
        }

        if (!isInsideLadder)
            currentLadder = null;
    }

    void StartLadderTopExit(Ladder ladder)
    {
        Vector3 topWorld = ladder.GetTopWorldPoint();

        isOnLadder = false;
        isExitingLadderTop = false;
        isInsideLadder = false;
        currentLadder = null;

        currentVelocity = Vector3.zero;
        yVelocity = 0f;
        isSprinting = false;

        ladderReenterTimer = ladderTopExitReenterBlockTime;

        ledgeBlockTimer = 0f;
        StartHang(topWorld);
    }

    void HandleLadderTopExit()
    {
        isExitingLadderTop = false;
    }

    void HandleLadderMovement()
    {
        if (currentLadder == null)
        {
            isOnLadder = false;
            return;
        }

        isSprinting = false;

        Vector3 ladderUp = currentLadder.GetUpDirection();
        float verticalInput = moveInput.y;

        if (currentLadder.snapPlayerToLadder && !currentLadder.IsAboveTop(transform.position))
        {
            Vector3 snapOffset = currentLadder.GetSnapOffset(transform.position);
            controller.Move(snapOffset * currentLadder.snapSpeed * Time.deltaTime);
        }

        float climbAmount = 0f;

        if (verticalInput > 0.01f)
        {
            climbAmount = ladderClimbUpSpeed;
        }
        else if (verticalInput < -0.01f)
        {
            bool fastSlideDown = sprintHeld;
            climbAmount = fastSlideDown ? -ladderFastDownSpeed : -ladderClimbDownSpeed;
        }
        else
        {
            climbAmount = ladderStayStillWhenIdle ? 0f : 0f;
        }

        Vector3 climbMotion = ladderUp * climbAmount;
        controller.Move(climbMotion * Time.deltaTime);

        currentVelocity = Vector3.zero;
        yVelocity = 0f;

        if (verticalInput > 0.1f && currentLadder.IsAboveTop(transform.position))
        {
            ExitLadder(false, true);
            return;
        }

        if (verticalInput < -0.1f && currentLadder.IsBelowBottom(transform.position))
        {
            ExitLadder(false, false);
            return;
        }

        if (!isInsideLadder && Mathf.Abs(verticalInput) < 0.01f)
        {
            ExitLadder(false, false);
            return;
        }
    }

    void UpdateStealthStatus()
    {
        if (stealthStatus == null)
            return;

        bool shouldCountAsMoving =
            moveInput.magnitude > 0.1f &&
            !isHanging &&
            !isDroppingToHang;

        stealthStatus.SetMoving(shouldCountAsMoving);
        stealthStatus.SetSprint(isSprinting);
        stealthStatus.SetCrouch(isCrouching);
        stealthStatus.SetGrounded(isGrounded);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (_capturedLock) return;
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (_capturedLock) return;

        sprintHeld = context.ReadValueAsButton();

        if (!sprintHeld)
            shiftGraceTimer = slideShiftGraceTime;

        if (isOnLadder || isExitingLadderTop || isClimbing || isHanging || isDroppingToHang || isPullingUp || isCrouching)
        {
            isSprinting = false;
            return;
        }

        UpdateSprintState();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (_capturedLock) return;
        if (!context.performed) return;

        if (isOnLadder)
        {
            ExitLadder(true, false);

            if (stealthStatus != null)
                stealthStatus.TriggerJumpNoise();

            FootstepAudio footsteps =
                GetComponent<FootstepAudio>();

            if (footsteps != null)
                footsteps.PlayJumpSound();

            return;
        }

        if (isSliding && isGrounded)
        {
            DoSlideJump();
            return;
        }

        if (isGrounded && !isCrouching)
        {
            yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (stealthStatus != null)
                stealthStatus.TriggerJumpNoise();

            FootstepAudio footsteps =
                GetComponent<FootstepAudio>();

            if (footsteps != null)
                footsteps.PlayJumpSound();

            return;
        }

        if (isWallSliding && wallJumpCooldownTimer <= 0f)
        {
            DoWallJump();
        }
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (_capturedLock) return;

        if (isOnLadder || isExitingLadderTop)
        {
            crouchHeld = false;
            return;
        }

        if (context.performed)
        {
            bool canSlide = (isSprinting || shiftGraceTimer > 0f) && isGrounded && !isSliding && slideCooldownTimer <= 0f;

            if (canSlide)
                StartSlide();
            else
                crouchHeld = true;
        }
        else if (context.canceled)
        {
            crouchHeld = false;

            if (isSliding)
                StopSlide();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(ladderTag)) return;

        Ladder ladder = other.GetComponent<Ladder>();
        if (ladder == null)
            ladder = other.GetComponentInParent<Ladder>();

        if (ladder != null)
        {
            if (ladderReenterTimer > 0f)
                return;

            currentLadder = ladder;
            isInsideLadder = true;
            ledgeBlockTimer = ledgeBlockAfterLadderTime;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(ladderTag)) return;

        if (ladderReenterTimer > 0f)
            return;

        if (currentLadder == null)
        {
            Ladder ladder = other.GetComponent<Ladder>();
            if (ladder == null)
                ladder = other.GetComponentInParent<Ladder>();

            if (ladder != null)
                currentLadder = ladder;
        }

        isInsideLadder = true;
        ledgeBlockTimer = ledgeBlockAfterLadderTime;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(ladderTag)) return;

        Ladder ladder = other.GetComponent<Ladder>();
        if (ladder == null)
            ladder = other.GetComponentInParent<Ladder>();

        if (ladder == currentLadder)
        {
            isInsideLadder = false;
            ledgeBlockTimer = ledgeBlockAfterLadderTime;

            if (!isOnLadder)
                currentLadder = null;
        }
    }

    public void ResetMovementState()
    {
        moveInput = Vector2.zero;
        currentVelocity = Vector3.zero;
        sprintHeld = false;
        isSprinting = false;
    }

    public void ResetAllStates()
    {
        moveInput = Vector2.zero;
        sprintHeld = false;
        isSprinting = false;
        crouchHeld = false;

        currentVelocity = Vector3.zero;
        yVelocity = -2f;

        isClimbing = false;
        isPullingUp = false;
        pullUpForceCrouch = false;
        ledgeGrabThisFrame = false;
        ledgeBlockTimer = 0f;
        hangTimer = 0f;
        hangTimeout = 0f;
        climbingTimeout = 0f;
        pullUpTimeout = 0f;

        isSliding = false;
        slideSpeed = 0f;
        slideCooldownTimer = 0f;
        shiftGraceTimer = 0f;

        isWallSliding = false;
        lastWallNormal = Vector3.zero;
        wallJumpCooldownTimer = 0f;

        isOnLadder = false;
        isInsideLadder = false;
        isExitingLadderTop = false;
        currentLadder = null;
        ladderReenterTimer = 0f;

        isCrouching = false;
        controller.height = standingHeight;
        controller.center = standingCenter;

        stepTimer = 0f;

        Debug.Log("[PlayerMovement] Estado resetado.");
    }

    public bool IsClimbing()
    {
        return isClimbing || isHanging || isDroppingToHang || isPullingUp || isOnLadder || isExitingLadderTop;
    }

    public bool IsSprinting() => isSprinting;
    public bool IsMoving() => moveInput.magnitude > 0.1f;
    public bool IsCrouching() => isCrouching;
    public bool IsOnLadder() => isOnLadder;
    public bool IsSliding() => isSliding;
    public bool IsWallSliding() => isWallSliding;
}