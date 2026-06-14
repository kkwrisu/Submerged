using UnityEngine;
using UnityEngine.AI;

public class NPCAnimator : MonoBehaviour
{
    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 2f;
    public float waitTime = 2f;
    public float reachDistance = 0.8f;

    [Header("Animation")]
    public float animationSpeedReference = 4.5f;

    [Header("Idle Gestures")]
    public float gestureIntervalMin = 6f;
    public float gestureIntervalMax = 14f;
    public bool useAnnoyedGesture = true;

    [Header("Dialogue")]
    public float lookAtPlayerSpeed = 5f;

    [Header("Head Look At")]
    [Range(0f, 1f)] public float headLookWeight = 0.6f;

    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;

    private bool isInDialogue;

    private int patrolIndex;
    private float waitTimer;
    private float idleTimer;
    private float gestureInterval;

    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashTurningL = Animator.StringToHash("TurningLeft");
    private static readonly int HashTurningR = Animator.StringToHash("TurningRight");
    private static readonly int HashAnnoyed = Animator.StringToHash("Annoyed");
    private static readonly int HashLookout = Animator.StringToHash("Lookout");
    private static readonly int HashTalking = Animator.StringToHash("Talking");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        gestureInterval = Random.Range(gestureIntervalMin, gestureIntervalMax);
    }

    private void Start()
    {
        GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
        if (foundPlayer != null)
            player = foundPlayer.transform;

        if (agent == null || patrolPoints == null || patrolPoints.Length == 0) return;
        agent.speed = patrolSpeed;
        GoTo();
    }

    private void OnEnable()
    {
        DialogueManager.OnDialogueEnded += HandleDialogueEnded;
    }

    private void OnDisable()
    {
        DialogueManager.OnDialogueEnded -= HandleDialogueEnded;
    }

    private void HandleDialogueEnded(Interactable interactable)
    {
        if (interactable == GetComponent<Interactable>())
            OnDialogueEnd();
    }

    private void Update()
    {
        if (isInDialogue)
        {
            animator?.SetFloat(HashSpeed, 0f, 0.1f, Time.deltaTime);
            FacePlayer();
            return;
        }

        UpdatePatrol();
        UpdateAnimator();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;

        if (isInDialogue && player != null)
        {
            animator.SetLookAtWeight(headLookWeight);
            animator.SetLookAtPosition(player.position + Vector3.up * 1.5f);
        }
        else
        {
            animator.SetLookAtWeight(0f);
        }
    }

    public void OnDialogueStart()
    {
        isInDialogue = true;
        agent.isStopped = true;
        animator?.SetTrigger(HashTalking);
    }

    public void OnDialogueEnd()
    {
        isInDialogue = false;
        agent.isStopped = false;
        GoTo();
    }

    private void FacePlayer()
    {
        if (player == null) return;

        Vector3 dir = (player.position - transform.position);
        dir.y = 0f;
        if (dir == Vector3.zero) return;

        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, lookAtPlayerSpeed * Time.deltaTime);
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

        float speed = agent != null ? agent.velocity.magnitude / animationSpeedReference : 0f;
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