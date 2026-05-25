using UnityEngine;

public class PlayerFootstepController : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private FootstepAudio footsteps;

    private void Awake()
    {
        playerMovement =
            GetComponent<PlayerMovement>();

        footsteps =
            GetComponent<FootstepAudio>();
    }

    private void Update()
    {
        if (playerMovement == null ||
            footsteps == null)
        {
            return;
        }

        // climbing = sem passos
        if (playerMovement.IsClimbing())
        {
            footsteps.currentState =
                FootstepAudio.MovementState.Idle;

            return;
        }

        // crouch silencioso
        if (playerMovement.IsCrouching())
        {
            footsteps.currentState =
                FootstepAudio.MovementState.Idle;

            return;
        }

        // parado
        if (!playerMovement.IsMoving())
        {
            footsteps.currentState =
                FootstepAudio.MovementState.Idle;

            return;
        }

        // correndo
        if (playerMovement.IsSprinting())
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