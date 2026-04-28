using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance;

    [Header("UI")]
    public GameObject pauseMenuPanel;
    public GameObject settingsPanel;

    [Header("Player Lock")]
    public MonoBehaviour playerMovementScript;
    public MonoBehaviour playerLookScript;
    public MonoBehaviour playerInteractScript;

    [Header("Scene")]
    public string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(ReconnectAfterFrame());
    }

    private IEnumerator ReconnectAfterFrame()
    {
        yield return null;
        yield return null; // segundo frame de segurança para builds

        Debug.Log("GameUI.Instance: " + GameUI.Instance);

        if (GameUI.Instance != null)
        {
            pauseMenuPanel = GameUI.Instance.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "PauseMenu")?.gameObject;

            settingsPanel = GameUI.Instance.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "Settings")?.gameObject;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Debug.Log("Player encontrado: " + player);

        if (player != null)
        {
            playerMovementScript = player.GetComponent<PlayerMovement>();
            playerLookScript = player.GetComponent<PlayerLook>();
            playerInteractScript = player.GetComponent<PlayerInteract>();

            PlayerInteract interact = player.GetComponent<PlayerInteract>();
            if (interact != null && GameUI.Instance != null)
            {
                UnityEngine.UI.Graphic crosshair = GameUI.Instance.GetComponentsInChildren<UnityEngine.UI.Graphic>(true)
                    .FirstOrDefault(g => g.name == "Crosshair");

                if (crosshair != null)
                    interact.crosshairGraphic = crosshair;
            }

            DialogueManager dialogue = player.GetComponentInChildren<DialogueManager>();
            Debug.Log("DialogueManager encontrado: " + dialogue);

            if (dialogue != null && GameUI.Instance != null)
            {
                var panel = GameUI.Instance.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name == "DialoguePanel");
                Debug.Log("DialoguePanel encontrado: " + panel);

                var text = GameUI.Instance.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true)
                    .FirstOrDefault(t => t.name == "DialogueText");
                Debug.Log("DialogueText encontrado: " + text);

                var choices = GameUI.Instance.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name == "ChoicesContainer");
                Debug.Log("ChoicesContainer encontrado: " + choices);

                var crosshairGraphic = GameUI.Instance.GetComponentsInChildren<UnityEngine.UI.Graphic>(true)
                    .FirstOrDefault(g => g.name == "Crosshair");
                Debug.Log("Crosshair encontrado: " + crosshairGraphic);

                dialogue.dialoguePanel = panel?.gameObject;
                dialogue.dialogueText = text;
                dialogue.choicesContainer = choices;

                if (crosshairGraphic != null)
                    dialogue.crosshair = crosshairGraphic.gameObject;

                dialogue.playerMovementScript = player.GetComponent<PlayerMovement>();
                dialogue.playerLookScript = player.GetComponent<PlayerLook>();
            }
        }

        ReconnectButtons();
    }

    private void ReconnectButtons()
    {
        if (GameUI.Instance == null) return;

        UnityEngine.UI.Button resumeButton = GameUI.Instance.GetComponentsInChildren<UnityEngine.UI.Button>(true)
            .FirstOrDefault(b => b.name == "Resume");

        UnityEngine.UI.Button settingsButton = GameUI.Instance.GetComponentsInChildren<UnityEngine.UI.Button>(true)
            .FirstOrDefault(b => b.name == "SettingsButton");

        UnityEngine.UI.Button backButton = GameUI.Instance.GetComponentsInChildren<UnityEngine.UI.Button>(true)
            .FirstOrDefault(b => b.name == "Back");

        UnityEngine.UI.Button mainMenuButton = GameUI.Instance.GetComponentsInChildren<UnityEngine.UI.Button>(true)
            .FirstOrDefault(b => b.name == "MainMenu");

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OpenSettings);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(CloseSettings);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
    }

    private void Start()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSettings();
            return;
        }

        if (isPaused)
        {
            ResumeGame();
            return;
        }

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
            return;

        PauseGame();
    }

    public void PauseGame()
    {
        if (isPaused)
            return;

        isPaused = true;
        Time.timeScale = 0f;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        if (playerLookScript != null)
            playerLookScript.enabled = false;

        if (playerInteractScript != null)
            playerInteractScript.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        isPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (playerMovementScript != null)
            playerMovementScript.enabled = true;

        if (playerLookScript != null)
            playerLookScript.enabled = true;

        if (playerInteractScript != null)
            playerInteractScript.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenSettings()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public bool IsPaused()
    {
        return isPaused;
    }
}