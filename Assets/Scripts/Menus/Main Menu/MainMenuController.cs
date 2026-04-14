using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Main Menu Elements")]
    public GameObject title;
    public GameObject playButton;
    public GameObject continueButton;
    public GameObject settingsButton;
    public GameObject quitButton;

    [Header("Continue Button (Button Component)")]
    public Button continueButtonComponent;

    [Header("Settings Panel")]
    public GameObject settingsPanel;

    [Header("Primeira cena do jogo")]
    public string firstSceneName = "NomeDaSuaCena";

    private void Start()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        ShowMainMenuElements(true);
        UpdateContinueButton();
    }

    public void PlayGame()
    {
        Debug.Log("New Game clicado");

        if (SaveManager.Instance != null)
        {
            Debug.Log("Limpando save...");
            SaveManager.Instance.DeleteSave();
        }
        else
        {
            Debug.LogWarning("SaveManager.Instance está null.");
        }

        if (string.IsNullOrWhiteSpace(firstSceneName))
        {
            Debug.LogError("firstSceneName está vazio.");
            return;
        }

        Debug.Log("Carregando cena: " + firstSceneName);
        SceneManager.LoadScene(firstSceneName);
    }

    public void ContinueGame()
    {
        if (SaveManager.Instance == null)
            return;

        if (!SaveManager.Instance.HasSave())
        {
            Debug.Log("Nenhum save disponível.");
            return;
        }

        SaveManager.Instance.LoadGameFromDisk();
        SaveManager.Instance.LoadSavedScene();
    }

    public void OpenSettings()
    {
        ShowMainMenuElements(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        ShowMainMenuElements(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Debug.Log("Saindo do jogo...");
        Application.Quit();
    }

    private void ShowMainMenuElements(bool show)
    {
        if (title != null) title.SetActive(show);
        if (playButton != null) playButton.SetActive(show);
        if (continueButton != null) continueButton.SetActive(show);
        if (settingsButton != null) settingsButton.SetActive(show);
        if (quitButton != null) quitButton.SetActive(show);
    }

    private void UpdateContinueButton()
    {
        if (continueButtonComponent == null)
            return;

        bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasSave();

        continueButtonComponent.interactable = hasSave;
    }
}