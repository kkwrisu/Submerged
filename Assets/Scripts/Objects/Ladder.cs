using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Ladder : MonoBehaviour
{
    [Header("Ladder Settings")]
    public float climbSpeed = 3.5f;
    public bool snapPlayerToLadder = true;
    public float snapSpeed = 10f;

    [Header("Exit Settings")]
    public float topExitOffset = 0.35f;
    public float bottomExitOffset = 0.1f;
    public float topExitForwardOffset = 0.45f;
    public float topExitUpOffset = 0.1f;

    private Collider ladderCollider;

    private void Awake()
    {
        ladderCollider = GetComponent<Collider>();
    }

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        if (!CompareTag("Ladder"))
            gameObject.tag = "Ladder";
    }

    public Vector3 GetUpDirection()
    {
        return transform.up.normalized;
    }

    public Vector3 GetForwardDirection()
    {
        return transform.forward.normalized;
    }

    public Vector3 GetClosestPoint(Vector3 worldPosition)
    {
        return ladderCollider.ClosestPoint(worldPosition);
    }

    public Vector3 GetSnapOffset(Vector3 playerPosition)
    {
        Vector3 up = GetUpDirection();
        Vector3 closest = GetClosestPoint(playerPosition);
        Vector3 delta = closest - playerPosition;

        return Vector3.ProjectOnPlane(delta, up);
    }

    public float GetTopPointAlongLadder()
    {
        Vector3 up = GetUpDirection();
        Bounds bounds = ladderCollider.bounds;

        Vector3 topWorld = bounds.center + Vector3.up * bounds.extents.y;
        return Vector3.Dot(topWorld, up);
    }

    public float GetBottomPointAlongLadder()
    {
        Vector3 up = GetUpDirection();
        Bounds bounds = ladderCollider.bounds;

        Vector3 bottomWorld = bounds.center - Vector3.up * bounds.extents.y;
        return Vector3.Dot(bottomWorld, up);
    }

    public bool IsAboveTop(Vector3 worldPosition)
    {
        float playerPoint = Vector3.Dot(worldPosition, GetUpDirection());
        return playerPoint >= GetTopPointAlongLadder() - topExitOffset;
    }

    public bool IsBelowBottom(Vector3 worldPosition)
    {
        float playerPoint = Vector3.Dot(worldPosition, GetUpDirection());
        return playerPoint <= GetBottomPointAlongLadder() + bottomExitOffset;
    }

    public Vector3 GetTopExitPosition(Vector3 currentPlayerPosition)
    {
        Vector3 up = GetUpDirection();
        Vector3 forward = GetForwardDirection();
        Vector3 pos = currentPlayerPosition;

        float currentAlong = Vector3.Dot(pos, up);
        float targetAlong = GetTopPointAlongLadder() + topExitUpOffset;
        float deltaAlong = targetAlong - currentAlong;

        return pos + up * deltaAlong + forward * topExitForwardOffset;
    }
}