using UnityEngine;

public class Progresso_Player : MonoBehaviour
{
    private void Start()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.RegisterPlayer(transform);
    }
}