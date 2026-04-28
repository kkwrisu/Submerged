using UnityEngine;

public class Doors : MonoBehaviour, ISaveable
{
    [Header("Save")]
    [SerializeField] private string saveID;

    [Header("Locks")]
    [Range(0, 2)]
    public int totalLocks = 2;

    [SerializeField] private bool[] unlockedLocks = new bool[2];

    [Header("Door")]
    public Transform doorVisual;
    public Vector3 closedPosition;
    public Vector3 openPosition;
    public float moveSpeed = 4f;

    [Header("Auto Open")]
    public Transform player;
    public float openDistance = 3f;
    public bool closeWhenPlayerLeaves = true;

    [Header("Gizmos")]
    public bool showGizmos = true;
    public Color lockedGizmoColor = new Color(1f, 0f, 0f, 0.25f);
    public Color unlockedGizmoColor = new Color(0f, 1f, 0f, 0.25f);
    public Color wireGizmoColor = Color.white;

    [Header("State")]
    public bool isOpen = false;

    private Vector3 targetPosition;

    void Start()
    {
        if (doorVisual == null)
            doorVisual = transform;

        if (unlockedLocks == null || unlockedLocks.Length != totalLocks)
            unlockedLocks = new bool[totalLocks];

        targetPosition = isOpen ? openPosition : closedPosition;
        doorVisual.localPosition = targetPosition;
    }

    void Update()
    {
        if (doorVisual == null)
            return;

        doorVisual.localPosition = Vector3.MoveTowards(
            doorVisual.localPosition,
            targetPosition,
            Time.deltaTime * moveSpeed
        );

        if (!AllLocksReleased())
            return;

        if (player == null)
            return;

        float distance = Vector3.Distance(player.position, transform.position);

        if (distance <= openDistance)
        {
            if (!isOpen)
                OpenDoor();
        }
        else
        {
            if (closeWhenPlayerLeaves && isOpen)
                CloseDoor();
        }
    }

    public void InteractWithDoor()
    {
        Debug.Log("Essa porta não abre por interação direta.");
    }

    public void UnlockLock(int lockIndex)
    {
        if (lockIndex < 0 || lockIndex >= totalLocks)
        {
            Debug.LogWarning("Índice de trava inválido: " + lockIndex);
            return;
        }

        if (unlockedLocks[lockIndex])
        {
            Debug.Log("A trava " + lockIndex + " já foi destravada.");
            return;
        }

        unlockedLocks[lockIndex] = true;
        Debug.Log("Trava " + lockIndex + " liberada.");

        if (AllLocksReleased())
            Debug.Log("Todas as travas da porta foram liberadas.");

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();
    }

    public bool IsLockReleased(int lockIndex)
    {
        if (lockIndex < 0 || lockIndex >= totalLocks)
            return false;

        return unlockedLocks[lockIndex];
    }

    public bool AllLocksReleased()
    {
        for (int i = 0; i < totalLocks; i++)
        {
            if (!unlockedLocks[i])
                return false;
        }

        return true;
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
        targetPosition = isOpen ? openPosition : closedPosition;

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();
    }

    public void OpenDoor()
    {
        if (!AllLocksReleased())
        {
            Debug.Log("Não é possível abrir: ainda existem travas.");
            return;
        }

        isOpen = true;
        targetPosition = openPosition;
    }

    public void CloseDoor()
    {
        isOpen = false;
        targetPosition = closedPosition;
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;

        Gizmos.color = AllLocksReleased() ? unlockedGizmoColor : lockedGizmoColor;
        Gizmos.DrawSphere(transform.position, openDistance);

        Gizmos.color = wireGizmoColor;
        Gizmos.DrawWireSphere(transform.position, openDistance);

        if (doorVisual != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, doorVisual.position);
        }
    }

    public string GetSaveID() => saveID;

    public void SaveToData(SaveData data)
    {
        for (int i = 0; i < data.doors.Count; i++)
        {
            if (data.doors[i].id == saveID)
            {
                data.doors[i] = new DoorSaveRecord
                {
                    id = saveID,
                    isOpen = isOpen,
                    unlockedLocks = (bool[])unlockedLocks.Clone()
                };
                return;
            }
        }

        data.doors.Add(new DoorSaveRecord
        {
            id = saveID,
            isOpen = isOpen,
            unlockedLocks = (bool[])unlockedLocks.Clone()
        });
    }

    public void LoadFromSave(SaveData data)
    {
        for (int i = 0; i < data.doors.Count; i++)
        {
            if (data.doors[i].id == saveID)
            {
                isOpen = data.doors[i].isOpen;

                if (data.doors[i].unlockedLocks != null)
                    unlockedLocks = (bool[])data.doors[i].unlockedLocks.Clone();

                targetPosition = isOpen ? openPosition : closedPosition;

                if (doorVisual != null)
                    doorVisual.localPosition = targetPosition;

                return;
            }
        }
    }
}