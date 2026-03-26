using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [Header("Referências")]
    public Transform playerBody;
    public Camera cam;

    [Header("Sensibilidade")]
    public float sensitivity = 100f;

    [Header("Smooth Look")]
    public float smoothTime = 0.05f;
    private Vector2 currentLook;
    private Vector2 lookVelocity;

    [Header("Head Bob")]
    public float bobFrequency = 6f;
    public float bobAmplitude = 0.05f;
    private float bobTimer;
    private Vector3 defaultPos;

    [Header("FOV")]
    public float normalFOV = 60f;
    public float sprintFOV = 75f;
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

    private Vector3 climbOffset;
    private float currentTilt;

    private float joltTimer;
    private float shakeTimer;

    private bool wasClimbing;
    private bool wasPullingUp;

    private PlayerMovement playerMovement;

    private Vector2 lookInput;
    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        defaultPos = transform.localPosition;
        playerMovement = GetComponentInParent<PlayerMovement>();

        if (cam != null)
            cam.fieldOfView = normalFOV;
    }

    void Update()
    {
        Look();
        HandleHeadBob();
        HandleFOV();
        HandleClimbCamera();
        HandleImpactFX();
    }

    void Look()
    {
        currentLook = Vector2.SmoothDamp(currentLook, lookInput, ref lookVelocity, smoothTime);

        float mouseX = currentLook.x * sensitivity * Time.deltaTime;
        float mouseY = currentLook.y * sensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation + currentTilt, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    void HandleHeadBob()
    {
        if (playerMovement != null && playerMovement.IsClimbing())
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, defaultPos + climbOffset, Time.deltaTime * climbSmooth);
            return;
        }

        if (currentLook.magnitude > 0.1f || (playerMovement != null && playerMovement.IsMoving()))
        {
            bobTimer += Time.deltaTime * bobFrequency;
            float bobOffset = Mathf.Sin(bobTimer) * bobAmplitude;

            transform.localPosition = defaultPos + new Vector3(0, bobOffset, 0);
        }
        else
        {
            bobTimer = 0;
            transform.localPosition = Vector3.Lerp(transform.localPosition, defaultPos, Time.deltaTime * 5f);
        }
    }

    void HandleClimbCamera()
    {
        if (playerMovement == null) return;

        bool isClimbing = playerMovement.IsClimbing();

        if (isClimbing && !wasClimbing)
        {
            joltTimer = 1f;
        }

        if (isClimbing)
        {
            float t = Mathf.Clamp01((transform.localPosition.y - defaultPos.y) / -climbYOffset);
            float ease = Mathf.Sin(t * Mathf.PI * 0.5f);

            climbOffset = new Vector3(0, climbYOffset * ease, 0);

            float bob = Mathf.Sin(Time.time * 10f) * 0.04f;
            climbOffset.y += bob;

            currentTilt = Mathf.Lerp(currentTilt, climbTilt, Time.deltaTime * climbSmooth);
        }
        else
        {
            climbOffset = Vector3.zero;
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
        {
            shakeTimer = 1f;
        }

        if (shakeTimer > 0f)
        {
            float shake = Mathf.Sin(Time.time * pullUpShakeSpeed) * pullUpShakeAmount * shakeTimer;
            transform.localPosition += new Vector3(shake, -shake, 0);

            shakeTimer -= Time.deltaTime * 6f;
        }

        wasPullingUp = isPullingUp;
    }

    void HandleFOV()
    {
        if (cam == null || playerMovement == null) return;

        float targetFOV = playerMovement.IsSprinting() ? sprintFOV : normalFOV;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * fovSpeed);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }
}