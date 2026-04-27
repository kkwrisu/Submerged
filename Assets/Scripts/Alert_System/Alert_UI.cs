using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Alert_UI : MonoBehaviour
{
    [Header("Fill Image")]
    public Image fillImage;

    [Header("Cenas onde deve ficar oculto")]
    public string[] hiddenInScenes = { "MainMenu", "MinigameScene" };

    private void Start()
    {
        CheckVisibility(SceneManager.GetActiveScene().name);
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (DungeonAlertSystem.Instance != null)
        {
            DungeonAlertSystem.Instance.onAlertChanged.AddListener(UpdateFill);
            Refresh();
        }
        else
        {
            if (fillImage != null)
            {
                Vector3 scale = fillImage.transform.localScale;
                scale.x = 0f;
                fillImage.transform.localScale = scale;
            }
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
        if (fillImage == null) return;

        float normalized = DungeonAlertSystem.Instance != null
            ? DungeonAlertSystem.Instance.AlertNormalized
            : 0f;

        Vector3 scale = fillImage.transform.localScale;
        scale.x = normalized;
        fillImage.transform.localScale = scale;
    }
}