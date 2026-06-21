using System.Collections;
using UnityEngine;

public class TeleportTrigger : MonoBehaviour
{
    [Header("Teleporte")]
    public Transform destination;

    [Header("Fade")]
    public float fadeDuration = 0.3f;

    [Header("Diálogo pós-teleporte (opcional)")]
    public Interactable postTeleportDialogue;

    private bool _teleporting = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (_teleporting) return;

        StartCoroutine(TeleportRoutine(other.transform));
    }

    private IEnumerator TeleportRoutine(Transform player)
    {
        _teleporting = true;

        if (SceneTransition.Instance != null)
            yield return SceneTransition.Instance.FadeInOnly(fadeDuration);

        if (destination != null)
            player.position = destination.position;

        if (SceneTransition.Instance != null)
            yield return SceneTransition.Instance.FadeOutOnly(fadeDuration);

        if (postTeleportDialogue != null && DialogueManager.Instance != null)
            DialogueManager.Instance.StartDialogue(postTeleportDialogue);

        _teleporting = false;
    }
}