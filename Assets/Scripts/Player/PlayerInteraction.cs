using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerInteract : MonoBehaviour
{
    [Header("Config")]
    public float interactDistance = 3f;
    public LayerMask interactMask = ~0;

    [Header("Delay")]
    public float interactCooldown = 0.2f;
    private float nextInteractTime = 0f;

    [Header("Referência")]
    public Camera cam;

    [Header("Crosshair")]
    public Graphic crosshairGraphic;
    public Color normalCrosshairColor = Color.white;
    public Color interactableCrosshairColor = Color.green;

    private Interactable currentTarget;
    private Interactable lastLoggedTarget;
    private CharacterController characterController;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
        {
            currentTarget = null;
            UpdateCrosshairColor(false);
            return;
        }

        CheckInteraction();
    }

    void CheckInteraction()
    {
        currentTarget = null;

        if (cam == null)
        {
            Debug.LogWarning("Camera não atribuída no PlayerInteract.");
            UpdateCrosshairColor(false);
            return;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.red);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask))
        {
            Interactable interactable = hit.collider.GetComponentInParent<Interactable>();

            if (interactable != null)
            {
                currentTarget = interactable;
            }
        }

        UpdateCrosshairColor(currentTarget != null);

        if (currentTarget != lastLoggedTarget)
        {
            if (currentTarget != null)
                Debug.Log("Novo alvo detectado: " + currentTarget.name);
            else
                Debug.Log("Nenhum alvo detectado.");

            lastLoggedTarget = currentTarget;
        }
    }

    void UpdateCrosshairColor(bool isInteractable)
    {
        if (crosshairGraphic == null)
            return;

        crosshairGraphic.color = isInteractable ? interactableCrosshairColor : normalCrosshairColor;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (Time.time < nextInteractTime)
            return;

        Debug.Log("Tecla de interação pressionada.");

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
        {
            Debug.Log("Diálogo já está ativo.");
            return;
        }

        if (characterController != null && !characterController.isGrounded)
        {
            Debug.Log("Não é possível interagir enquanto estiver no ar.");
            return;
        }

        if (currentTarget != null)
        {
            Debug.Log("Chamando Interact() em: " + currentTarget.name);
            currentTarget.Interact();

            nextInteractTime = Time.time + interactCooldown;
        }
        else
        {
            Debug.LogWarning("Nenhum currentTarget encontrado.");
        }
    }

    public Interactable GetCurrentTarget()
    {
        return currentTarget;
    }
}