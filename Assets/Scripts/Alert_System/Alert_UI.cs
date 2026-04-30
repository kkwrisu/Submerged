using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Alert_UI : MonoBehaviour
{
    [Header("Slider de Alerta")]
    public Slider alertSlider;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (alertSlider != null)
        {
            alertSlider.minValue = 0f;
            alertSlider.maxValue = 1f;
            alertSlider.interactable = false;
        }

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
            if (alertSlider != null)
                alertSlider.value = 0f;
        }
    }

    private void DisconnectAlertSystem()
    {
        if (DungeonAlertSystem.Instance != null)
            DungeonAlertSystem.Instance.onAlertChanged.RemoveListener(UpdateFill);
    }

    // GameUI é responsável pelo SetActive deste objeto.
    // Este script só zera o valor quando não há sistema de alerta na cena.
    private void ResetValueIfNoSystem()
    {
        if (alertSlider != null && DungeonAlertSystem.Instance == null)
            alertSlider.value = 0f;
    }

    private void UpdateFill(float value) => Refresh();

    private void Refresh()
    {
        if (alertSlider == null) return;

        alertSlider.value = DungeonAlertSystem.Instance != null
            ? DungeonAlertSystem.Instance.AlertNormalized
            : 0f;
    }
}