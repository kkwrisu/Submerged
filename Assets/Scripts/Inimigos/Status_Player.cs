using UnityEngine;

public class Player_Status : MonoBehaviour
{
    [Header("State (runtime)")]
    public bool isMoving;
    public bool isSprinting;
    public bool isCrouching;
    public bool isGrounded = true;

    [Header("Noise Values")]
    public float idleNoise = 0f;
    public float walkNoise = 0.35f;
    public float sprintNoise = 1f;
    public float crouchNoise = 0.12f;
    public float airNoise = 0.55f;
    public float landingNoise = 0.8f;

    [Header("Auto Movement Detection")]
    public bool autoDetectMovement = true;
    public float movementThreshold = 0.05f;

    private Vector3 lastPosition;
    private bool firstFrame = true;

    private float temporaryNoise;
    private float temporaryNoiseTimer;

    public float CurrentNoise
    {
        get
        {
            if (temporaryNoiseTimer > 0f)
                return temporaryNoise;

            if (!isMoving)
                return idleNoise;

            if (isCrouching)
                return crouchNoise;

            if (isSprinting)
                return sprintNoise;

            return walkNoise;
        }
    }

    private void Update()
    {
        if (autoDetectMovement)
            DetectMovement();

        if (temporaryNoiseTimer > 0f)
            temporaryNoiseTimer -= Time.deltaTime;
    }

    private void DetectMovement()
    {
        if (firstFrame)
        {
            firstFrame = false;
            lastPosition = transform.position;
            isMoving = false;
            return;
        }

        float movedDistance = Vector3.Distance(transform.position, lastPosition);
        isMoving = movedDistance > movementThreshold;

        lastPosition = transform.position;
    }

    public void SetSprint(bool value)
    {
        isSprinting = value;
    }

    public void SetCrouch(bool value)
    {
        isCrouching = value;
    }

    public void SetGrounded(bool value)
    {
        isGrounded = value;
    }

    public void SetMoving(bool value)
    {
        isMoving = value;
    }

    // =========================
    // Ruídos especiais
    // =========================

    public void TriggerJumpNoise()
    {
        MakeTemporaryNoise(airNoise, 0.3f);
    }

    public void TriggerLandingNoise()
    {
        MakeTemporaryNoise(landingNoise, 0.4f);
    }

    public void MakeTemporaryNoise(float noiseValue, float duration)
    {
        temporaryNoise = noiseValue;
        temporaryNoiseTimer = duration;
    }
}