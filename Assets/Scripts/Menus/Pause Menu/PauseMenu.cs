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
    private bool waitingForEscRelease = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // Start roda depois de todos os Awakes da cena — seguro pra buscar referências
        StartCoroutine(ReconnectAfterFrame());

        Time.timeScale = 1f;
        isPaused = false;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (waitingForEscRelease)
        {
            if (Keyboard.current != null && !Keyboard.current.escapeKey.isPressed)
                waitingForEscRelease = false;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Só dispara em troca de cena (não na primeira — essa o Start resolve)
        StartCoroutine(ReconnectAfterFrame());
    }

    private IEnumerator ReconnectAfterFrame()
    {
        // Espera GameUI estar pronto de verdade
        float timeout = 5f;
        float elapsed = 0f;
        while (GameUI.Instance == null && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (GameUI.Instance == null)
        {
            Debug.LogError("[PauseMenu] GameUI.Instance ainda null após timeout — abortando reconnect.");
            yield break;
        }

        Debug.Log("[PauseMenu] Reconectando. GameUI: " + GameUI.Instance);

        pauseMenuPanel = GameUI.Instance.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "PauseMenu")?.gameObject;

        settingsPanel = GameUI.Instance.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "Settings")?.gameObject;

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        Debug.Log("[PauseMenu] pauseMenuPanel: " + pauseMenuPanel);
        Debug.Log("[PauseMenu] settingsPanel: " + settingsPanel);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Debug.Log("[PauseMenu] Player: " + player);

        if (player != null)
        {
            playerMovementScript = player.GetComponent<PlayerMovement>();
            playerLookScript = player.GetComponent<PlayerLook>();
            playerInteractScript = player.GetComponent<PlayerInteract>();

            // Busca PlayerLook nos filhos também caso não esteja no root
            if (playerLookScript == null)
                playerLookScript = player.GetComponentInChildren<PlayerLook>();

            Debug.Log("[PauseMenu] PlayerMovement: " + playerMovementScript);
            Debug.Log("[PauseMenu] PlayerLook: " + playerLookScript);
            Debug.Log("[PauseMenu] PlayerInteract: " + playerInteractScript);

            PlayerInteract interact = player.GetComponent<PlayerInteract>();
            if (interact != null)
            {
                UnityEngine.UI.Graphic crosshair = GameUI.Instance
                    .GetComponentsInChildren<UnityEngine.UI.Graphic>(true)
                    .FirstOrDefault(g => g.name == "Crosshair");

                if (crosshair != null)
                    interact.crosshairGraphic = crosshair;
            }

            DialogueManager dialogue = player.GetComponentInChildren<DialogueManager>();
            Debug.Log("[PauseMenu] DialogueManager: " + dialogue);

            if (dialogue != null)
            {
                var panel = GameUI.Instance.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name == "DialoguePanel");

                var text = GameUI.Instance.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true)
                    .FirstOrDefault(t => t.name == "DialogueText");

                var choices = GameUI.Instance.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name == "ChoicesContainer");

                var crosshairGraphic = GameUI.Instance
                    .GetComponentsInChildren<UnityEngine.UI.Graphic>(true)
                    .FirstOrDefault(g => g.name == "Crosshair");

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

        var allButtons = GameUI.Instance.GetComponentsInChildren<UnityEngine.UI.Button>(true);
        Debug.Log($"[PauseMenu] Botões encontrados ({allButtons.Length}):");
        foreach (var b in allButtons)
            Debug.Log($"  - '{b.name}'");

        UnityEngine.UI.Button resumeButton = allButtons.FirstOrDefault(b => b.name == "Resume");
        UnityEngine.UI.Button settingsButton = allButtons.FirstOrDefault(b => b.name == "SettingsButton");
        UnityEngine.UI.Button backButton = allButtons.FirstOrDefault(b => b.name == "Back");
        UnityEngine.UI.Button mainMenuButton = allButtons.FirstOrDefault(b =>
            b.name == "Main Menu" || b.name == "MainMenu" ||
            b.name == "main menu" || b.name == "main_menu" ||
            b.name == "BtnMainMenu");

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

        Debug.Log("[PauseMenu] Resume: " + (resumeButton != null ? "OK" : "NULL"));
        Debug.Log("[PauseMenu] Settings: " + (settingsButton != null ? "OK" : "NULL"));
        Debug.Log("[PauseMenu] Back: " + (backButton != null ? "OK" : "NULL"));
        Debug.Log("[PauseMenu] MainMenu: " + (mainMenuButton != null ? "OK" : "NULL"));
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        TogglePause();
    }

    private void TogglePause()
    {
        if (RepairPuzzleManager.Instance != null && RepairPuzzleManager.Instance.IsPuzzleOpen()) return;
        if (waitingForEscRelease) return;
        if (CutsceneManager.Instance != null && CutsceneManager.Instance.IsActive()) return;

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

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive()) return;

        PauseGame();
    }

    public void BlockPauseUntilEscReleased() => waitingForEscRelease = true;
    public void BlockPauseForOneFrame() => waitingForEscRelease = true;

    public void PauseGame()
    {
        if (RepairPuzzleManager.Instance != null && RepairPuzzleManager.Instance.IsPuzzleOpen()) return;
        if (waitingForEscRelease) return;
        if (isPaused) return;

        isPaused = true;
        Time.timeScale = 0f;

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (playerMovementScript != null) playerMovementScript.enabled = false;
        if (playerLookScript != null) playerLookScript.enabled = false;
        if (playerInteractScript != null) playerInteractScript.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (playerMovementScript != null) playerMovementScript.enabled = true;
        if (playerLookScript != null) playerLookScript.enabled = true;
        if (playerInteractScript != null) playerInteractScript.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenSettings()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
    }

    public void ReturnToMainMenu()
    {
        Debug.Log("[PauseMenu] ReturnToMainMenu chamado.");
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public bool IsPaused() => isPaused;
}