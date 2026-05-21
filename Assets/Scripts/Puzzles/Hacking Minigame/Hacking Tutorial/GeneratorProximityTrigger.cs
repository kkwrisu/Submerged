using UnityEngine;

/// <summary>
/// Coloque este componente (com um Collider2D trigger) no GameObject do gerador
/// ou num filho. Dispara o tutorial de aproximação quando o jogador entra.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GeneratorProximityTrigger : MonoBehaviour
{
    [Tooltip("Tag do jogador.")]
    public string playerTag = "Player";

    [Tooltip("Referência ao tracker de tutorial do gerador.")]
    public GeneratorTutorialTracker tutorialTracker;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag(playerTag)) return;

        triggered = true; // evita disparo duplo por frame

        if (tutorialTracker != null)
            tutorialTracker.TryShowApproachTutorial();
    }
}