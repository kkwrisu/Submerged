using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Alert_UI : MonoBehaviour
{
    [Header("Barras de Alerta")]
    [Tooltip("Sprite base com todas as barras apagadas — sempre visível.")]
    public Image baseImage;

    [Tooltip("8 Images, uma por barra, em ordem crescente de alerta. Arraste da menor para a maior.")]
    public Image[] barImages = new Image[8];

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        ResetValueIfNoSystem();
        ConnectAlertSystem();
    }

    private void OnDestroy()
    {
        DisconnectAlertSystem();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DisconnectAlertSystem();
        ResetValueIfNoSystem();
        ConnectAlertSystem();
    }

    private void ConnectAlertSystem()
    {
        if (DungeonAlertSystem.Instance != null)
        {
            DungeonAlertSystem.Instance.onAlertChanged.AddListener(UpdateFill);
            Refresh();
        }
        else
        {
            SetActiveBars(0);
        }
    }

    private void DisconnectAlertSystem()
    {
        if (DungeonAlertSystem.Instance != null)
            DungeonAlertSystem.Instance.onAlertChanged.RemoveListener(UpdateFill);
    }

    private void ResetValueIfNoSystem()
    {
        if (DungeonAlertSystem.Instance == null)
            SetActiveBars(0);
    }

    private void UpdateFill(float value) => Refresh();

    private void Refresh()
    {
        float normalized = DungeonAlertSystem.Instance != null
            ? DungeonAlertSystem.Instance.AlertNormalized
            : 0f;

        // Cada barra representa 12.5% — quantas barras devem estar acesas
        int activeBars = Mathf.RoundToInt(normalized * 8f);
        SetActiveBars(activeBars);
    }

    private void SetActiveBars(int count)
    {
        for (int i = 0; i < barImages.Length; i++)
        {
            if (barImages[i] != null)
                barImages[i].enabled = i < count;
        }
    }
}