using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Alert_UI : MonoBehaviour
{
    [Header("Slider de Alerta")]
    public Slider alertSlider;

    [Header("Cenas onde deve ficar oculto")]
    public string[] hiddenInScenes = { "MainMenu", "MinigameScene" };

    private void Start()
    {
        if (alertSlider != null)
        {
            alertSlider.minValue = 0f;
            alertSlider.maxValue = 1f;
            alertSlider.interactable = false;
        }

        CheckVisibility(SceneManager.GetActiveScene().name);
        SceneManager.sceneLoaded += OnSceneLoaded;

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

    private void OnDestroy()
    {
        if (DungeonAlertSystem.Instance != null)
            DungeonAlertSystem.Instance.onAlertChanged.RemoveListener(UpdateFill);

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckVisibility(scene.name);
    }

    private void CheckVisibility(string sceneName)
    {
        bool shouldHide = System.Array.Exists(hiddenInScenes, s => s == sceneName);
        gameObject.SetActive(!shouldHide);
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