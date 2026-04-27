using UnityEngine;
using UnityEngine.InputSystem;

public class GeneratorInteractable : Interactable
{
    [Header("Minigame")]
    public GeneratorMinigame minigame;

    [Header("Input Actions")]
    public InputActionReference holdAction;
    public InputActionReference qteAction;

    [Header("Player References")]
    public Transform playerTransform;
    public float maxDistance = 3.5f;

    private bool playerHolding;
    private bool interactionStarted;

    private InputAction hold;
    private InputAction qte;

    private void Awake()
    {
        if (minigame == null)
            minigame = GetComponent<GeneratorMinigame>();

        hold = holdAction?.action;
        qte = qteAction?.action;
    }

    private void OnEnable()
    {
        hold?.Enable();
        qte?.Enable();

        if (hold != null)
        {
            hold.performed += OnHoldPerformed;
            hold.canceled += OnHoldCanceled;
        }

        if (qte != null)
            qte.performed += OnQTEPerformed;
    }

    private void OnDisable()
    {
        if (hold != null)
        {
            hold.performed -= OnHoldPerformed;
            hold.canceled -= OnHoldCanceled;
        }

        if (qte != null)
            qte.performed -= OnQTEPerformed;

        hold?.Disable();
        qte?.Disable();

        ForceExit();
    }

    private void Update()
    {
        if (!interactionStarted) return;

        // Distância
        if (playerTransform != null)
        {
            float dist = Vector3.Distance(playerTransform.position, transform.position);

            if (dist > maxDistance)
            {
                ForceExit();
                return;
            }
        }

        // Controle do minigame (único lugar que inicia)
        if (playerHolding && !minigame.IsActive() && !minigame.IsCompleted())
            minigame.StartMinigame();

        if (!playerHolding && minigame.IsActive())
            minigame.PauseMinigame();
    }

    public override void Interact()
    {
        if (minigame == null || minigame.IsCompleted()) return;

        interactionStarted = true;
        playerHolding = true;
    }

    private void OnHoldPerformed(InputAction.CallbackContext ctx)
    {
        if (!interactionStarted) return;
        playerHolding = true;
    }

    private void OnHoldCanceled(InputAction.CallbackContext ctx)
    {
        playerHolding = false;

        if (interactionStarted)
            minigame.PauseMinigame();
    }

    private void OnQTEPerformed(InputAction.CallbackContext ctx)
    {
        if (!interactionStarted || !playerHolding) return;
        minigame.OnQTEInput();
    }

    private void ForceExit()
    {
        if (!interactionStarted) return;

        interactionStarted = false;
        playerHolding = false;
        minigame?.ExitMinigame();
    }

    public bool IsBeingRepaired()
        => interactionStarted && minigame != null && minigame.IsActive();
}