using UnityEngine;
using UnityEngine.SceneManagement;

public class Checkpoint : MonoBehaviour
{
    [Header("Spawn Point")]
    public Transform spawnPoint;

    public void ActivateCheckpoint()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager não encontrado.");
            return;
        }

        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        float yRotation = spawnPoint != null ? spawnPoint.eulerAngles.y : transform.eulerAngles.y;

        SaveManager.Instance.SetCheckpoint(position, yRotation, SceneManager.GetActiveScene().name);
        Debug.Log("Checkpoint ativado.");
    }
}