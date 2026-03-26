using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    [Header("Config")]
    public float interactDistance = 3f;
    public string interactTag = "Interactable";

    [Header("Referência")]
    public Camera cam;

    private Interactable currentTarget;

    void Update()
    {
        CheckInteraction();
    }

    void CheckInteraction()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            if (hit.collider.CompareTag(interactTag))
            {
                Interactable interactable = hit.collider.GetComponent<Interactable>();

                if (interactable != null)
                {
                    currentTarget = interactable;
                    return;
                }
            }
        }

        currentTarget = null;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        Debug.Log("E foi pressionado");

        if (context.performed && currentTarget != null)
        {
            currentTarget.Interact();
        }
    }
}