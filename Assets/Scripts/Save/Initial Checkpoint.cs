using UnityEngine;
using UnityEngine.SceneManagement;

public class InitialCheckpoint : MonoBehaviour
{
    [Tooltip("Posição inicial do player. Se null, usa a posição deste GameObject.")]
    public Transform spawnPoint;

    private void Start()
    {
        // Só define se não houver save anterior (primeira vez ou save deletado)
        if (SaveManager.Instance == null) return;

        if (!SaveManager.Instance.HasSave())
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