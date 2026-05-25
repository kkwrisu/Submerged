using UnityEngine;

public class FootstepAudio : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Surface Mapping
    // -------------------------------------------------------------------------

    [System.Serializable]
    public struct SurfaceSound
    {
        [Tooltip("Tag do GameObject de chao")]
        public string tag;

        // ---------------------------------------------------------------------
        // WALK
        // ---------------------------------------------------------------------

        [Header("Walking")]
        public AudioClip[] walkClips;

        // ---------------------------------------------------------------------
        // RUN
        // ---------------------------------------------------------------------

        [Header("Running")]
        public AudioClip[] sprintClips;

        // ---------------------------------------------------------------------
        // JUMP
        // ---------------------------------------------------------------------

        [Header("Jump")]
        public AudioClip[] jumpClips;

        // ---------------------------------------------------------------------
        // LAND
        // ---------------------------------------------------------------------

        [Header("Landing")]
        public AudioClip[] landClips;
    }

    // -------------------------------------------------------------------------
    // Movement State
    // -------------------------------------------------------------------------

    public enum MovementState
    {
        Idle,
        Walking,
        Sprinting
    }

    // -------------------------------------------------------------------------
    // Audio Types
    // -------------------------------------------------------------------------

    private enum SurfaceAudioType
    {
        Walk,
        Sprint,
        Jump,
        Land
    }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Superficies")]
    public SurfaceSound[] surfaces;

    // -------------------------------------------------------------------------
    // Fallbacks
    // -------------------------------------------------------------------------

    [Header("Fallback Clips")]

    public AudioClip[] defaultWalkClips;
    public AudioClip[] defaultSprintClips;
    public AudioClip[] defaultJumpClips;
    public AudioClip[] defaultLandClips;

    // -------------------------------------------------------------------------
    // Raycast
    // -------------------------------------------------------------------------

    [Header("Raycast")]

    [Tooltip("Distancia maxima do raycast")]
    public float rayDistance = 1.2f;

    [Tooltip("Layers consideradas chao")]
    public LayerMask groundLayers = ~0;

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------

    [Header("Audio")]

    public AudioSource audioSource;

    [Tooltip("Variacao aleatoria de pitch")]
    public bool randomizePitch = true;

    [Tooltip("Pitch minimo")]
    public float minPitch = 0.96f;

    [Tooltip("Pitch maximo")]
    public float maxPitch = 1.04f;

    // -------------------------------------------------------------------------
    // Intervals
    // -------------------------------------------------------------------------

    [Header("Intervalos")]

    public float walkInterval = 0.48f;
    public float sprintInterval = 0.31f;

    // -------------------------------------------------------------------------
    // Volume
    // -------------------------------------------------------------------------

    [Header("Volume")]

    [Range(0f, 1f)]
    public float walkVolume = 0.42f;

    [Range(0f, 1f)]
    public float sprintVolume = 0.72f;

    [Range(0f, 1f)]
    public float jumpVolume = 0.45f;

    [Range(0f, 1f)]
    public float landVolume = 0.75f;

    // -------------------------------------------------------------------------
    // Runtime
    // -------------------------------------------------------------------------

    [HideInInspector]
    public MovementState currentState;

    private float stepTimer;

    private bool wasGrounded;

    // -------------------------------------------------------------------------
    // Unity
    // -------------------------------------------------------------------------

    private void Update()
    {
        HandleJumpAndLandingSounds();
        HandleFootsteps();
    }

    // -------------------------------------------------------------------------
    // Footsteps
    // -------------------------------------------------------------------------

    private void HandleFootsteps()
    {
        if (currentState == MovementState.Idle)
        {
            stepTimer = 0f;
            return;
        }

        bool sprinting =
            currentState == MovementState.Sprinting;

        float interval =
            sprinting
                ? sprintInterval
                : walkInterval;

        float volume =
            sprinting
                ? sprintVolume
                : walkVolume;

        stepTimer += Time.deltaTime;

        if (stepTimer >= interval)
        {
            stepTimer = 0f;

            PlaySurfaceSound(
                sprinting
                    ? SurfaceAudioType.Sprint
                    : SurfaceAudioType.Walk,
                volume
            );
        }
    }

    // -------------------------------------------------------------------------
    // Jump / Landing
    // -------------------------------------------------------------------------

    private void HandleJumpAndLandingSounds()
    {
        bool grounded = IsGrounded();

        // saiu do chão
        if (wasGrounded && !grounded)
        {
            PlaySurfaceSound(
                SurfaceAudioType.Jump,
                jumpVolume
            );
        }

        // tocou no chão
        if (!wasGrounded && grounded)
        {
            PlaySurfaceSound(
                SurfaceAudioType.Land,
                landVolume
            );
        }

        wasGrounded = grounded;
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(
            transform.position,
            Vector3.down,
            rayDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    // -------------------------------------------------------------------------
    // Surface Detection
    // -------------------------------------------------------------------------

    private AudioClip[] GetClipsForSurface(
        SurfaceAudioType type
    )
    {
        if (Physics.Raycast(
                transform.position,
                Vector3.down,
                out RaycastHit hit,
                rayDistance,
                groundLayers,
                QueryTriggerInteraction.Ignore))
        {
            foreach (SurfaceSound s in surfaces)
            {
                if (!hit.collider.CompareTag(s.tag))
                    continue;

                switch (type)
                {
                    // ---------------------------------------------------------
                    // WALK
                    // ---------------------------------------------------------

                    case SurfaceAudioType.Walk:

                        if (s.walkClips != null &&
                            s.walkClips.Length > 0)
                        {
                            return s.walkClips;
                        }

                        break;

                    // ---------------------------------------------------------
                    // SPRINT
                    // ---------------------------------------------------------

                    case SurfaceAudioType.Sprint:

                        if (s.sprintClips != null &&
                            s.sprintClips.Length > 0)
                        {
                            return s.sprintClips;
                        }

                        break;

                    // ---------------------------------------------------------
                    // JUMP
                    // ---------------------------------------------------------

                    case SurfaceAudioType.Jump:

                        if (s.jumpClips != null &&
                            s.jumpClips.Length > 0)
                        {
                            return s.jumpClips;
                        }

                        break;

                    // ---------------------------------------------------------
                    // LAND
                    // ---------------------------------------------------------

                    case SurfaceAudioType.Land:

                        if (s.landClips != null &&
                            s.landClips.Length > 0)
                        {
                            return s.landClips;
                        }

                        break;
                }
            }
        }

        // ---------------------------------------------------------------------
        // FALLBACKS
        // ---------------------------------------------------------------------

        switch (type)
        {
            case SurfaceAudioType.Walk:
                return defaultWalkClips;

            case SurfaceAudioType.Sprint:
                return defaultSprintClips;

            case SurfaceAudioType.Jump:
                return defaultJumpClips;

            case SurfaceAudioType.Land:
                return defaultLandClips;
        }

        return null;
    }

    // -------------------------------------------------------------------------
    // Playback
    // -------------------------------------------------------------------------

    private void PlaySurfaceSound(
        SurfaceAudioType type,
        float volume
    )
    {
        if (audioSource == null)
            return;

        AudioClip[] clips =
            GetClipsForSurface(type);

        if (clips == null || clips.Length == 0)
            return;

        AudioClip clip =
            clips[Random.Range(0, clips.Length)];

        if (randomizePitch)
        {
            audioSource.pitch =
                Random.Range(minPitch, maxPitch);
        }
        else
        {
            audioSource.pitch = 1f;
        }

        audioSource.PlayOneShot(
            clip,
            volume
        );
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public void PlayJumpSound()
    {
        PlaySurfaceSound(
            SurfaceAudioType.Jump,
            jumpVolume
        );
    }

    public void PlayLandingSound()
    {
        PlaySurfaceSound(
            SurfaceAudioType.Land,
            landVolume
        );
    }

    // -------------------------------------------------------------------------
    // Debug
    // -------------------------------------------------------------------------

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawLine(
            transform.position,
            transform.position +
            Vector3.down * rayDistance
        );
    }

#endif
}