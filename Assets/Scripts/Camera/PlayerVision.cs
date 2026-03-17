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
    }

    void Look()
    {
        currentLook = Vector2.SmoothDamp(currentLook, lookInput, ref lookVelocity, smoothTime);

        float mouseX = currentLook.x * sensitivity * Time.deltaTime;
        float mouseY = currentLook.y * sensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    void HandleHeadBob()
    {
        // Se estiver se movendo (mouse ou movimento do player)
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