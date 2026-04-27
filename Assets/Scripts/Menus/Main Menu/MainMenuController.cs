using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Main Menu Elements")]
    public GameObject title;
    public GameObject playButton;
    public GameObject continueButton;
    public GameObject settingsButton;
    public GameObject creditsButton;
    public GameObject quitButton;
    public GameObject menuBackground;

    [Header("Continue Button (Button Component)")]
    public Button continueButtonComponent;

    [Header("Settings Panel")]
    public GameObject settingsPanel;

    [Header("Credits Panel")]
    public GameObject creditsPanel;

    [Header("Primeira cena do jogo")]
    public string firstSceneName = "NomeDaSuaCena";

    private void Start()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (creditsPanel != null)
            creditsPanel.SetActive(false);

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

        if (SceneTransition.Instance == null)
        {
            Debug.LogError("SceneTransition.Instance está null.");
            return;
        }

        Debug.Log("Carregando cena com transição: " + firstSceneName);
        SceneTransition.Instance.TransitionToScene(firstSceneName);
    }

    public void ContinueGame()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("SaveManager.Instance está null.");
            return;
        }

        if (!SaveManager.Instance.HasSave())
        {
            Debug.Log("Nenhum save disponível.");
            return;
        }

        SaveManager.Instance.LoadGameFromDisk();
        string savedSceneName = SaveManager.Instance.GetSavedSceneName();

        if (string.IsNullOrWhiteSpace(savedSceneName))
        {
            Debug.LogWarning("Nenhuma cena salva encontrada.");
            return;
        }

        if (SceneTransition.Instance == null)
        {
            Debug.LogError("SceneTransition.Instance está null.");
            return;
        }

        Debug.Log("Continuando jogo com transição para: " + savedSceneName);
        SceneTransition.Instance.TransitionToScene(savedSceneName);
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

    public void OpenCredits()
    {
        ShowMainMenuElements(false);

        if (creditsPanel != null)
            creditsPanel.SetActive(true);
    }

    public void CloseCredits()
    {
        ShowMainMenuElements(true);

        if (creditsPanel != null)
            creditsPanel.SetActive(false);
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
        if (creditsButton != null) creditsButton.SetActive(show);
        if (quitButton != null) quitButton.SetActive(show);
        if (menuBackground != null) menuBackground.SetActive(show);
    }

    private void UpdateContinueButton()
    {
        if (continueButtonComponent == null)
            return;

        bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasSave();
        continueButtonComponent.interactable = hasSave;
    }
}