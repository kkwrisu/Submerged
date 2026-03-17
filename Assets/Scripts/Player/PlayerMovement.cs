using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;
    private Vector2 moveInput;

    [Header("Movimento")]
    public float speed = 5f;
    public float sprintSpeed = 8f;

    [Header("Pulo & Gravidade")]
    public float gravity = -20f;
    public float fallMultiplier = 2.5f;
    public float jumpHeight = 1.3f;

    private float yVelocity;
    private bool isSprinting;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    private bool isGrounded;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        // Checa chão
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && yVelocity < 0)
            yVelocity = -2f;

        // Movimento horizontal
        float currentSpeed = isSprinting ? sprintSpeed : speed;
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // Gravidade estilo AAA (queda mais rápida)
        if (yVelocity < 0)
        {
            yVelocity += gravity * fallMultiplier * Time.deltaTime;
        }
        else
        {
            yVelocity += gravity * Time.deltaTime;
        }

        // Aplica movimento vertical
        controller.Move(Vector3.up * yVelocity * Time.deltaTime);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        isSprinting = context.ReadValueAsButton();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded)
        {
            yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    public bool IsSprinting()
    {
        return isSprinting;
    }

    public bool IsMoving()
    {
        return moveInput.magnitude > 0.1f;
    }
}