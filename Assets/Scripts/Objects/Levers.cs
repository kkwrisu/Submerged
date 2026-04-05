using UnityEngine;

public class Levers : MonoBehaviour
{
    [Header("Door")]
    public Doors targetDoor;
    public int lockIndex = 0;

    [Header("Lever")]
    public Transform leverVisual;
    public Vector3 offRotation;
    public Vector3 onRotation = new Vector3(-60f, 0f, 0f);
    public float leverMoveSpeed = 6f;

    [Header("Colors")]
    public ChangeColor changeColor;
    public Color deactivatedColor = Color.red;
    public Color activatedColor = Color.green;

    [Header("State")]
    public bool activated = false;

    private Quaternion targetRotation;

    void Start()
    {
        if (leverVisual == null)
            leverVisual = transform;

        targetRotation = Quaternion.Euler(activated ? onRotation : offRotation);
        leverVisual.localRotation = targetRotation;

        if (changeColor == null)
            changeColor = GetComponent<ChangeColor>();

        UpdateLeverColor();
    }

    void Update()
    {
        if (leverVisual == null)
            return;

        leverVisual.localRotation = Quaternion.Lerp(
            leverVisual.localRotation,
            targetRotation,
            Time.deltaTime * leverMoveSpeed
        );
    }

    public void ActivateLever()
    {
        if (activated)
        {
            Debug.Log("Essa alavanca já foi ativada.");
            return;
        }

        activated = true;
        targetRotation = Quaternion.Euler(onRotation);
        UpdateLeverColor();

        if (targetDoor != null)
        {
            targetDoor.UnlockLock(lockIndex);
        }
        else
        {
            Debug.LogWarning("DoorLever sem targetDoor.");
        }

        Debug.Log("Alavanca ativada.");
    }

    private void UpdateLeverColor()
    {
        if (changeColor == null)
            return;

        changeColor.SetColor(activated ? activatedColor : deactivatedColor);
    }
}