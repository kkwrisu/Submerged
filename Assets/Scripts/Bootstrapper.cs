using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrapper : MonoBehaviour
{
    [Header("Managers")]
    public GameObject saveManagerPrefab;
    public GameObject sceneTransitionPrefab;
    public GameObject audioManagerPrefab;
    public GameObject captureHandlerPrefab;

    [Header("First Scene")]
    public string firstSceneName = "MainMenu";

    private void Awake()
    {
        if (saveManagerPrefab != null) Instantiate(saveManagerPrefab);
        if (sceneTransitionPrefab != null) Instantiate(sceneTransitionPrefab);
        if (audioManagerPrefab != null) Instantiate(audioManagerPrefab);
        if (captureHandlerPrefab != null) Instantiate(captureHandlerPrefab);

        SceneManager.LoadScene(firstSceneName);
    }
}