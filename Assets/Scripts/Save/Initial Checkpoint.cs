using UnityEngine;
using UnityEngine.SceneManagement;

public class InitialCheckpoint : MonoBehaviour
{
    [Tooltip("Posição inicial do player. Se null, usa a posição deste GameObject.")]
    public Transform spawnPoint;

    private void Start()
    {
        if (SaveManager.Instance == null) return;

        string currentScene = SceneManager.GetActiveScene().name;
        string savedScene = SaveManager.Instance.GetSavedSceneName();

        // Define o checkpoint inicial se: não há save nenhum ainda,
        // OU o save existente não pertence a esta cena (logo não há
        // checkpoint válido aqui ainda).
        if (!SaveManager.Instance.HasSave() || savedScene != currentScene)
            Activate();
    }

    public void Activate()
    {
        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        float yRotation = spawnPoint != null ? spawnPoint.eulerAngles.y : transform.eulerAngles.y;

        SaveManager.Instance.SetCheckpoint(position, yRotation, SceneManager.GetActiveScene().name);
        Debug.Log("[InitialCheckpoint] Checkpoint inicial definido.");
    }
}