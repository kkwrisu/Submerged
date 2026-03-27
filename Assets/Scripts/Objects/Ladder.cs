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

    private float GetExtentAlongDirection(Vector3 direction)
    {
        Bounds bounds = ladderCollider.bounds;
        Vector3 extents = bounds.extents;

        Vector3 dir = new Vector3(
            Mathf.Abs(direction.x),
            Mathf.Abs(direction.y),
            Mathf.Abs(direction.z)
        );

        return extents.x * dir.x + extents.y * dir.y + extents.z * dir.z;
    }

    public Vector3 GetTopWorldPoint()
    {
        Vector3 up = GetUpDirection();
        Bounds bounds = ladderCollider.bounds;

        float extentAlongUp = GetExtentAlongDirection(up);
        return bounds.center + up * extentAlongUp;
    }

    public Vector3 GetBottomWorldPoint()
    {
        Vector3 up = GetUpDirection();
        Bounds bounds = ladderCollider.bounds;

        float extentAlongUp = GetExtentAlongDirection(up);
        return bounds.center - up * extentAlongUp;
    }

    public float GetTopPointAlongLadder()
    {
        return Vector3.Dot(GetTopWorldPoint(), GetUpDirection());
    }

    public float GetBottomPointAlongLadder()
    {
        return Vector3.Dot(GetBottomWorldPoint(), GetUpDirection());
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
        Vector3 topWorld = GetTopWorldPoint();

        return topWorld
             + forward * topExitForwardOffset
             + up * topExitUpOffset;
    }
}