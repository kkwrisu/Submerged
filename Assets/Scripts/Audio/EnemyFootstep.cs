using UnityEngine;
using UnityEngine.AI;

public class EnemyFootstepController : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;

    public FootstepAudio footsteps;

    // -------------------------------------------------------------------------
    // Movement Threshold
    // -------------------------------------------------------------------------

    [Header("Movement")]

    [Tooltip("Velocidade minima pra considerar movimento")]
    public float movementThreshold = 0.1f;

    [Tooltip("Velocidade considerada sprint")]
    public float sprintThreshold = 4f;

    // -------------------------------------------------------------------------
    // Unity
    // -------------------------------------------------------------------------

    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();

        footsteps = GetComponent<FootstepAudio>();
    }

    private void Update()
    {
        if (agent == null ||
            footsteps == null)
        {
            return;
        }

        float speed =
            agent.velocity.magnitude;

        // parado
        if (speed <= movementThreshold)
        {
            footsteps.currentState =
                FootstepAudio.MovementState.Idle;

            return;
        }

        // correndo
        if (speed >= sprintThreshold)
        {
            footsteps.currentState =
                FootstepAudio.MovementState.Sprinting;

            return;
        }

        // andando
        footsteps.currentState =
            FootstepAudio.MovementState.Walking;
    }
}