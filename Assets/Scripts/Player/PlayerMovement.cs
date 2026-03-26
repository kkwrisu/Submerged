using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;
    private Vector2 moveInput;

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
    private bool isSprinting;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;
    private bool isGrounded;

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
    public float hangDropDistance = 0.15f;
    public float hangDropSpeed = 8f;

    private float hangTimer;
    private bool isDroppingToHang;
    private float hangStartY;

    private bool isClimbing;
    private bool isHanging;
    private bool isPullingUp;

    private Vector3 climbTarget;
    private float climbStartY;
    private Vector3 pullUpTarget;

    [Header("Crouch")]
    public float crouchHeight = 1f;
    public float crouchTransitionSpeed = 12f;

    private bool crouchHeld;
    private bool isCrouching;

    private float standingHeight;
    private Vector3 standingCenter;
    private Vector3 crouchingCenter;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        standingHeight = controller.height;
        standingCenter = controller.center;

        float heightDifference = standingHeight - crouchHeight;
        crouchingCenter = standingCenter - Vector3.up * (heightDifference * 0.5f);
    }

    void Update()
    {
        stepTimer -= Time.deltaTime;
        hangTimer -= Time.deltaTime;

        HandleGroundCheck();
        HandleCrouch();

        if (!isGrounded && !isClimbing && !isHanging && !isDroppingToHang && !isPullingUp)
            CheckLedge();

        HandleHang();
        HandleClimb();
        HandlePullUp();
        HandleMovement();
    }

    void HandleGroundCheck()
    {
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
    }

    void HandleCrouch()
    {
        bool canCrouchNow = !isClimbing && !isHanging && !isDroppingToHang && !isPullingUp;

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

        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        Vector3 targetCenter = isCrouching ? crouchingCenter : standingCenter;

        controller.height = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        controller.center = Vector3.Lerp(controller.center, targetCenter, crouchTransitionSpeed * Time.deltaTime);

        if (isCrouching)
            isSprinting = false;
    }

    bool CanStandUp()
    {
        if (isClimbing || isHanging || isDroppingToHang || isPullingUp)
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

    void HandleMovement()
    {
        if (isClimbing || isHanging || isDroppingToHang || isPullingUp) return;

        float targetSpeed;

        if (isCrouching)
            targetSpeed = crouchSpeed;
        else
            targetSpeed = isSprinting ? sprintSpeed : walkSpeed;

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

        if (yVelocity < 0)
            yVelocity += gravity * fallMultiplier * Time.deltaTime;
        else if (yVelocity > 0)
            yVelocity += gravity * lowJumpMultiplier * Time.deltaTime;

        controller.Move(Vector3.up * yVelocity * Time.deltaTime);
    }

    void StepClimb(Vector3 moveDirection)
    {
        if (!isGrounded || isCrouching || moveInput.magnitude < 0.1f || stepTimer > 0)
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

    void StartHang(Vector3 ledgePoint)
    {
        climbTarget = ledgePoint;

        yVelocity = 0f;
        currentVelocity = Vector3.zero;

        hangTimer = hangInputDelay;
        hangStartY = transform.position.y;

        isDroppingToHang = true;
    }

    void HandleHang()
    {
        if (isDroppingToHang)
        {
            controller.Move(Vector3.down * hangDropSpeed * Time.deltaTime);

            if (transform.position.y <= hangStartY - hangDropDistance)
            {
                isDroppingToHang = false;
                isHanging = true;
            }

            return;
        }

        if (!isHanging) return;

        if (moveInput.y < -0.1f)
        {
            isHanging = false;
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
        isClimbing = true;
        climbTarget = ledgePoint;
        climbStartY = transform.position.y;
        yVelocity = 0f;
    }

    void HandleClimb()
    {
        if (!isClimbing) return;

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
        pullUpTarget = transform.position + transform.forward * pullUpForward + Vector3.up * pullUpUpward;
    }

    void HandlePullUp()
    {
        if (!isPullingUp) return;

        Vector3 toTarget = pullUpTarget - transform.position;
        controller.Move(toTarget.normalized * pullUpSpeed * Time.deltaTime);

        if (toTarget.magnitude < 0.05f)
        {
            isPullingUp = false;
            yVelocity = -2f;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (isCrouching)
        {
            isSprinting = false;
            return;
        }

        isSprinting = context.ReadValueAsButton();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded && !isCrouching)
        {
            yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.performed)
            crouchHeld = true;
        else if (context.canceled)
            crouchHeld = false;
    }

    public bool IsClimbing()
    {
        return isClimbing || isHanging || isDroppingToHang || isPullingUp;
    }

    public bool IsSprinting()
    {
        return isSprinting;
    }

    public bool IsMoving()
    {
        return moveInput.magnitude > 0.1f;
    }

    public bool IsCrouching()
    {
        return isCrouching;
    }
}