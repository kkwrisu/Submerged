using System.Collections;
using UnityEngine;

public class VoidFailsafe : MonoBehaviour
{
    [Header("Void Detection")]
    [Tooltip("Tempo em segundos caindo continuamente para considerar void.")]
    public float fallTimeThreshold = 5f;

    [Tooltip("Delay em segundos antes de respawnar.")]
    public float respawnDelay = 0.5f;

    private float fallTimer = 0f;
    private bool isRespawning = false;

    private CharacterController controller;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (isRespawning) return;

        if (playerMovement != null && playerMovement.IsClimbing())
        {
            fallTimer = 0f;
            return;
        }

        bool isFalling = controller != null
            ? controller.velocity.y < -0.5f
            : false;

        if (isFalling)
        {
            fallTimer += Time.deltaTime;

            if (fallTimer >= fallTimeThreshold)
            {
                StartCoroutine(RespawnRoutine());
                Debug.LogWarning("VoidFailsafe: Player voltou à posição normal.");
            }
        }
        else
        {
            fallTimer = 0f;
        }
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;
        fallTimer = 0f;

        yield return new WaitForSeconds(respawnDelay);

        if (SaveManager.Instance != null)
            SaveManager.Instance.RespawnPlayerAtCheckpoint();
        else
            Debug.LogWarning("VoidFailsafe: SaveManager não encontrado.");

        isRespawning = false;
    }
}