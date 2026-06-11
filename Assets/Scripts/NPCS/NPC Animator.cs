using UnityEngine;
using UnityEngine.AI;

public class NPCAnimator : MonoBehaviour
{
    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 2f;
    public float waitTime = 2f;
    public float reachDistance = 0.8f;

    [Header("Idle Gestures")]
    public float gestureIntervalMin = 6f;
    public float gestureIntervalMax = 14f;
    public bool useAnnoyedGesture = true;

    private NavMeshAgent agent;
    private Animator animator;

    private int patrolIndex;
    private float waitTimer;
    private float idleTimer;
    private float gestureInterval;

    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashTurningL = Animator.StringToHash("TurningLeft");
    private static readonly int HashTurningR = Animator.StringToHash("TurningRight");
    private static readonly int HashAnnoyed = Animator.StringToHash("Annoyed");
    private static readonly int HashLookout = Animator.StringToHash("Lookout");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        gestureInterval = Random.Range(gestureIntervalMin, gestureIntervalMax);
    }

    private void Start()
    {
        if (agent == null || patrolPoints == null || patrolPoints.Length == 0) return;
        agent.speed = patrolSpeed;
        GoTo();
    }

    private void Update()
    {
        UpdatePatrol();
        UpdateAnimator();
    }

    private void UpdatePatrol()
    {
        if (agent == null || patrolPoints == null || patrolPoints.Length == 0) return;
        if (agent.pathPending || agent.remainingDistance > reachDistance) return;

        waitTimer += Time.deltaTime;
        if (waitTimer >= waitTime)
        {
            waitTimer = 0f;
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            GoTo();
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        float speed = agent != null ? agent.velocity.magnitude / patrolSpeed : 0f;
        animator.SetFloat(HashSpeed, speed, 0.1f, Time.deltaTime);

        if (speed > 0.1f && agent != null)
        {
            float cross = Vector3.Cross(transform.forward, agent.velocity.normalized).y;
            float dot = Vector3.Dot(transform.forward, agent.velocity.normalized);
            animator.SetBool(HashTurningL, cross < -0.5f && dot < 0.5f);
            animator.SetBool(HashTurningR, cross > 0.5f && dot < 0.5f);
        }
        else
        {
            animator.SetBool(HashTurningL, false);
            animator.SetBool(HashTurningR, false);
        }

        if (speed < 0.05f)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= gestureInterval)
            {
                idleTimer = 0f;
                gestureInterval = Random.Range(gestureIntervalMin, gestureIntervalMax);
                animator.SetTrigger(useAnnoyedGesture ? HashAnnoyed : HashLookout);
            }
        }
        else
        {
            idleTimer = 0f;
        }
    }

    private void GoTo()
    {
        if (agent.enabled && agent.isOnNavMesh)
            agent.SetDestination(patrolPoints[patrolIndex].position);
    }
}