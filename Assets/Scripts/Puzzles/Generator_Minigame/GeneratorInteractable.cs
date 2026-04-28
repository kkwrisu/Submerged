using UnityEngine;
using UnityEngine.InputSystem;

public class GeneratorInteractable : Interactable
{
    [Header("Minigame")]
    public GeneratorMinigame minigame;

    [Header("Input — nomes das actions no seu InputActions asset")]
    public string holdActionName = "HoldInteract";
    public string qteActionName = "QTEInput";

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
    }

    private void OnEnable()
    {
        // Busca o PlayerInput no player (funciona igual aos seus outros scripts)
        if (playerTransform != null)
            BindActions(playerTransform.GetComponentInParent<PlayerInput>()
                     ?? playerTransform.GetComponent<PlayerInput>());
    }

    private void BindActions(PlayerInput pi)
    {
        if (pi == null) return;

        hold = pi.actions.FindAction(holdActionName, throwIfNotFound: false);
        qte = pi.actions.FindAction(qteActionName, throwIfNotFound: false);

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

        ForceExit();
    }

    private void Update()
    {
        if (!interactionStarted) return;

        if (playerTransform != null)
        {
            float dist = Vector3.Distance(playerTransform.position, transform.position);
            if (dist > maxDistance) { ForceExit(); return; }
        }

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
        if (interactionStarted) minigame.PauseMinigame();
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