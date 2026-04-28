using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [Header("Referências")]
    public Transform playerBody;
    public Camera cam;

    [Header("Sensibilidade")]
    public float sensitivity = 0.15f;

    [Header("Head Bob")]
    public float bobFrequency = 6f;
    public float bobAmplitude = 0.05f;
    private float bobTimer;
    private Vector3 defaultPos;

    [Header("FOV")]
    public float normalFOV = 60f;
    public float sprintFOV = 75f;
    public float crouchFOV = 55f;
    public float fovSpeed = 5f;

    [Header("Climb FX")]
    public float climbYOffset = -0.25f;
    public float climbTilt = 10f;
    public float climbSmooth = 6f;

    [Header("Impact FX")]
    public float climbJoltForce = 0.15f;
    public float climbJoltSpeed = 12f;

    [Header("Pull-Up Shake")]
    public float pullUpShakeAmount = 0.08f;
    public float pullUpShakeSpeed = 20f;

    [Header("Crouch Camera")]
    public float crouchYOffset = -0.5f;
    public float crouchSmooth = 10f;

    private Vector3 climbOffset;
    private Vector3 crouchOffset;
    private float currentTilt;

    private float joltTimer;
    private float shakeTimer;

    private bool wasClimbing;
    private bool wasPullingUp;

    private PlayerMovement playerMovement;

    private float xRotation = 0f;
    private Vector2 lookInput;

    private void OnEnable()
    {
        lookInput = Vector2.zero;
    }

    private void OnDisable()
    {
        lookInput = Vector2.zero;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        defaultPos = transform.localPosition;
        playerMovement = GetComponentInParent<PlayerMovement>();

        if (cam != null)
            cam.fieldOfView = normalFOV;
    }

    void Update()
    {
        if (PauseMenu.Instance != null && PauseMenu.Instance.IsPaused())
            return;

        Look();
        HandleCrouchCamera();
        HandleHeadBob();
        HandleFOV();
        HandleClimbCamera();
        HandleImpactFX();
    }

    void Look()
    {
        if (playerBody == null) return;

        float mouseX = lookInput.x * sensitivity;
        float mouseY = lookInput.y * sensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation + currentTilt, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    void HandleCrouchCamera()
    {
        if (playerMovement == null) return;

        Vector3 targetOffset = playerMovement.IsCrouching()
            ? new Vector3(0f, crouchYOffset, 0f)
            : Vector3.zero;

        crouchOffset = Vector3.Lerp(crouchOffset, targetOffset, Time.deltaTime * crouchSmooth);
    }

    void HandleHeadBob()
    {
        if (playerMovement != null && playerMovement.IsClimbing())
        {
            Vector3 targetPos = defaultPos + crouchOffset + climbOffset;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * climbSmooth);
            return;
        }

        Vector3 basePos = defaultPos + crouchOffset;

        if (playerMovement != null && playerMovement.IsMoving())
        {
            bobTimer += Time.deltaTime * bobFrequency;

            float bobOffsetY = Mathf.Sin(bobTimer) * bobAmplitude;
            float bobOffsetX = Mathf.Cos(bobTimer * 0.5f) * bobAmplitude * 0.35f;

            Vector3 bobPos = basePos + new Vector3(bobOffsetX, bobOffsetY, 0f);
            transform.localPosition = Vector3.Lerp(transform.localPosition, bobPos, Time.deltaTime * 10f);
        }
        else
        {
            bobTimer = 0f;
            transform.localPosition = Vector3.Lerp(transform.localPosition, basePos, Time.deltaTime * 8f);
        }
    }

    void HandleClimbCamera()
    {
        if (playerMovement == null) return;

        bool isClimbing = playerMovement.IsClimbing();

        if (isClimbing && !wasClimbing)
            joltTimer = 1f;

        if (isClimbing)
        {
            float bob = Mathf.Sin(Time.time * 10f) * 0.04f;
            climbOffset = new Vector3(0f, climbYOffset + bob, 0f);
            currentTilt = Mathf.Lerp(currentTilt, climbTilt, Time.deltaTime * climbSmooth);
        }
        else
        {
            climbOffset = Vector3.Lerp(climbOffset, Vector3.zero, Time.deltaTime * climbSmooth);
            currentTilt = Mathf.Lerp(currentTilt, 0f, Time.deltaTime * climbSmooth);
        }

        wasClimbing = isClimbing;
    }

    void HandleImpactFX()
    {
        if (playerMovement == null) return;

        if (joltTimer > 0f)
        {
            float jolt = Mathf.Sin(joltTimer * Mathf.PI * 0.5f) * climbJoltForce;
            transform.localPosition += Vector3.down * jolt;
            joltTimer -= Time.deltaTime * climbJoltSpeed;
        }

        bool isPullingUp = playerMovement.IsClimbing() && !wasClimbing;

        if (isPullingUp && !wasPullingUp)
            shakeTimer = 1f;

        if (shakeTimer > 0f)
        {
            float shake = Mathf.Sin(Time.time * pullUpShakeSpeed) * pullUpShakeAmount * shakeTimer;
            transform.localPosition += new Vector3(shake, -shake, 0f);
            shakeTimer -= Time.deltaTime * 6f;
        }

        wasPullingUp = isPullingUp;
    }

    void HandleFOV()
    {
        if (cam == null || playerMovement == null) return;

        float targetFOV = normalFOV;

        if (playerMovement.IsCrouching())
            targetFOV = crouchFOV;
        else if (playerMovement.IsSprinting())
            targetFOV = sprintFOV;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * fovSpeed);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }
}