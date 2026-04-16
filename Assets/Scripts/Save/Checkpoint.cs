using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Header("Spawn Point")]
    public Transform spawnPoint;

    private bool activated = false;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activated)
            return;

        if (!other.CompareTag("Player"))
            return;

        ActivateCheckpoint();
        activated = true;
    }

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