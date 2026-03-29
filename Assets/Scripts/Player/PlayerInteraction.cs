using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    [Header("Config")]
    public float interactDistance = 3f;
    public LayerMask interactMask = ~0;

    [Header("Referência")]
    public Camera cam;

    private Interactable currentTarget;

    void Update()
    {
        CheckInteraction();
    }

    void CheckInteraction()
    {
        currentTarget = null;

        if (cam == null)
        {
            Debug.LogWarning("Camera não atribuída no PlayerInteract.");
            return;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.red);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask))
        {
            Debug.Log("Raycast acertou: " + hit.collider.name);

            Interactable interactable = hit.collider.GetComponentInParent<Interactable>();

            if (interactable != null)
            {
                currentTarget = interactable;
                Debug.Log("Interactable detectado: " + interactable.name);
            }
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        Debug.Log("Tecla de interação pressionada.");

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
        {
            Debug.Log("Diálogo já está ativo.");
            return;
        }

        if (currentTarget != null)
        {
            Debug.Log("Chamando Interact() em: " + currentTarget.name);
            currentTarget.Interact();
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