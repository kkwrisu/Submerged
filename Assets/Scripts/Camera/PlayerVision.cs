using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [Header("Referencias")]
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

    [Header("Catch Knockback — Queda Pra Tras")]
    [Tooltip("Duracao total do efeito em segundos. Lido pelo Inimigo para sincronizar o respawn.")]
    public float knockbackDuration = 1.4f;

    [Tooltip("Graus para tras no pico do voo — fase 1 (impulso). Valor negativo = olha pro ceu.")]
    public float knockbackBackTilt = -55f;

    [Tooltip("Angulo final no chao — fase 2/3 (impacto). ~90 = camera olhando pro chao.")]
    public float knockbackGroundAngle = 92f;

    [Tooltip("Intensidade do tremor no impacto (fase 3).")]
    public float knockbackImpactShake = 0.12f;

    [Tooltip("Velocidade do tremor.")]
    public float knockbackShakeSpeed = 18f;

    [Tooltip("Quanto a camera desce verticalmente ao bater no chao.")]
    public float knockbackYDrop = -0.7f;

    private Vector3 climbOffset;
    private Vector3 crouchOffset;
    private float currentTilt;

    private float joltTimer;
    private float shakeTimer;

    private bool wasClimbing;
    private bool wasPullingUp;

    private float knockbackTimer = 0f;

    private PlayerMovement playerMovement;

    private float xRotation = 0f;
    private Vector2 lookInput;

    // Congela o Update() inteiro — usado pelo CaptureHandler durante o fade
    // para garantir que nenhum frame com estado sujo vaze para a tela.
    private bool _frozen = false;

    // -------------------------------------------------------------------------
    // Unity callbacks
    // -------------------------------------------------------------------------

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
        // Congelado pelo CaptureHandler enquanto a tela esta preta/resetando.
        // Nenhum sistema de camera roda — a rotacao permanece exatamente
        // como foi definida por ResetAfterCapture(), sem ser sobrescrita.
        if (_frozen) return;

        if (PauseMenu.Instance != null && PauseMenu.Instance.IsPaused())
            return;

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
            return;

        // O knockback tem prioridade sobre tudo — roda primeiro e sozinho
        if (knockbackTimer > 0f)
        {
            HandleKnockback();
            return;
        }

        Look();
        HandleCrouchCamera();
        HandleHeadBob();
        HandleFOV();
        HandleClimbCamera();
        HandleImpactFX();
    }

    // -------------------------------------------------------------------------
    // Camera / look
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // Knockback — queda pra tras com cabeçada no chao
    //
    // Fase 1 (0–30%): impulso pra tras  — camera vai olhar pro ceu
    // Fase 2 (30–55%): rotacao no ar    — cabeça cai em direcao ao chao
    // Fase 3 (55–100%): impacto + tremor — camera trava no chao e vibra
    // -------------------------------------------------------------------------

    /// <summary>
    /// Chame pelo UnityEvent onPlayerCaught do Inimigo.
    /// Inicia a animacao de queda pra tras com cabeçada no chao.
    /// </summary>
    public void TriggerKnockback()
    {
        knockbackTimer = knockbackDuration;
        xRotation = 0f;
    }

    private void HandleKnockback()
    {
        knockbackTimer -= Time.deltaTime;
        knockbackTimer = Mathf.Max(knockbackTimer, 0f);

        float t = 1f - (knockbackTimer / knockbackDuration);

        float tiltAngle;
        float yDrop = 0f;
        float shakeX = 0f;
        float shakeY = 0f;

        if (t < 0.30f)
        {
            float p = t / 0.30f;
            float eased = p * p;

            tiltAngle = Mathf.Lerp(0f, knockbackBackTilt, eased);
            yDrop = Mathf.Lerp(0f, 0.12f, eased);
        }
        else if (t < 0.55f)
        {
            float p = (t - 0.30f) / 0.25f;
            float eased = p * p * p;

            tiltAngle = Mathf.Lerp(knockbackBackTilt, knockbackGroundAngle, eased);
            yDrop = Mathf.Lerp(0.12f, knockbackYDrop * 0.55f, eased);
        }
        else
        {
            float p = (t - 0.55f) / 0.45f;
            float decay = 1f - p;

            tiltAngle = knockbackGroundAngle + Mathf.Sin(p * Mathf.PI * 5f) * 3.5f * decay;
            yDrop = Mathf.Lerp(knockbackYDrop * 0.55f, knockbackYDrop, Mathf.SmoothStep(0f, 1f, p * 0.6f));

            float impactShake = knockbackImpactShake * decay;
            shakeX = Mathf.Sin(Time.time * knockbackShakeSpeed * 1.3f) * impactShake;
            shakeY = Mathf.Sin(Time.time * knockbackShakeSpeed) * impactShake * 0.5f;
        }

        transform.localRotation = Quaternion.Euler(
            tiltAngle + shakeY * 10f,
            shakeX * 6f,
            shakeX * 3f
        );

        transform.localPosition = defaultPos + crouchOffset + new Vector3(
            shakeX * 0.03f,
            yDrop + shakeY * 0.015f,
            0f
        );

        if (knockbackTimer <= 0f)
        {
            transform.localPosition = defaultPos + crouchOffset;
            transform.localRotation = Quaternion.Euler(xRotation + currentTilt, 0f, 0f);
        }
    }

    // -------------------------------------------------------------------------
    // Reset pos-captura — chamado pelo CaptureHandler com a tela preta
    // -------------------------------------------------------------------------

    /// <summary>
    /// Congela o Update() e limpa todo o estado da camera.
    /// Chame isso logo apos o FadeOut completar (tela 100% preta).
    /// Chame Unfreeze() quando quiser devolver o controle (ex: apos FadeIn).
    /// </summary>
    public void FreezeAndReset()
    {
        _frozen = true;

        knockbackTimer = 0f;
        xRotation = 0f;
        currentTilt = 0f;
        climbOffset = Vector3.zero;
        crouchOffset = Vector3.zero;
        bobTimer = 0f;
        joltTimer = 0f;
        shakeTimer = 0f;
        lookInput = Vector2.zero;
        wasClimbing = false;
        wasPullingUp = false;

        // Aplica a rotacao neutra diretamente — sem Lerp, sem frame de delay
        transform.localPosition = defaultPos;
        transform.localRotation = Quaternion.identity;

        if (cam != null)
            cam.fieldOfView = normalFOV;
    }

    /// <summary>
    /// Devolve o controle da camera ao player.
    /// Chame apos o FadeIn completar.
    /// </summary>
    public void Unfreeze()
    {
        _frozen = false;
    }

    // -------------------------------------------------------------------------
    // Input
    // -------------------------------------------------------------------------

    public void OnLook(InputAction.CallbackContext context)
    {
        if (_frozen) return;
        if (knockbackTimer > 0f) return;
        lookInput = context.ReadValue<Vector2>();
    }
}