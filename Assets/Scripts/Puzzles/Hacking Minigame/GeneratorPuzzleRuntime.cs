using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gerencia toda a lógica do minigame de gerador estilo DBD.
/// Roda na cena de gameplay principal.
/// </summary>
public class GeneratorPuzzleRuntime : MonoBehaviour
{
    [Header("Progresso")]
    public float fillSpeed = 0.06f;
    public float failRegressAmount = 0.15f;
    public float releaseRegressSpeed = 0.03f;

    [Header("QTE")]
    public float qteMinInterval = 0.2f;
    public float qteMaxInterval = 0.4f;
    public float qteWindowSeconds = 1.2f;

    [Header("Player Lock")]
    public MonoBehaviour playerMovement;
    public MonoBehaviour playerLook;
    public PlayerInteract playerInteract;
    public UnityEngine.InputSystem.PlayerInput playerInput;
    public PlayerMovement playerMovementFull;

    [Header("Input")]
    public InputActionReference interactHoldAction;
    public InputActionReference qteInputAction;

    private GeneratorPuzzleUI _ui;

    private GeneratorPuzzleInteractable _currentInteractable;

    private float _progress = 0f;
    private bool _isInteracting = false;
    private bool _isOpen = false;        // true enquanto a UI está visível
    private bool _qteActive = false;
    private float _qteTimer = 0f;
    private float _nextQteThreshold = 0f;
    private bool _playerLocked = false;

    private InputAction _interactHold;
    private InputAction _qteInput;

    // ── Unity ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _ui = FindFirstObjectByType<GeneratorPuzzleUI>();
        _interactHold = interactHoldAction != null ? interactHoldAction.action : null;
        _qteInput = qteInputAction != null ? qteInputAction.action : null;
    }

    private void OnEnable()
    {
        _interactHold?.Enable();
        _qteInput?.Enable();
    }

    private void OnDisable()
    {
        _interactHold?.Disable();
        _qteInput?.Disable();
    }

    private void Update()
    {
        // Regressão silenciosa — UI fechada, apenas decai o progresso em memória
        if (!_isOpen)
        {
            if (_progress > 0f)
            {
                _progress -= releaseRegressSpeed * Time.deltaTime;
                _progress = Mathf.Clamp01(_progress);
            }
            return;
        }

        // ── QTE ativo ─────────────────────────────────────────────────────────
        if (_qteActive)
        {
            _qteTimer -= Time.deltaTime;
            _ui?.UpdateQteTimer(_qteTimer / qteWindowSeconds);

            bool qtePressed =
                (_qteInput != null && _qteInput.WasPressedThisFrame()) ||
                (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);

            if (qtePressed)
            {
                ResolveQTE(success: true);
                return;
            }

            if (_qteTimer <= 0f)
            {
                ResolveQTE(success: false);
                return;
            }

            return;
        }

        // ── Não está segurando: fecha UI e regride ────────────────────────────
        if (!_isInteracting)
        {
            CloseUI();
            return;
        }

        // ── Progresso ─────────────────────────────────────────────────────────
        _progress += fillSpeed * Time.deltaTime;
        _progress = Mathf.Clamp01(_progress);
        _ui?.SetProgress(_progress);

        if (_progress >= _nextQteThreshold && _nextQteThreshold < 2f)
        {
            TriggerQTE();
            return;
        }

        if (_progress >= 1f)
            CompletePuzzle();
    }

    // ── API Pública ────────────────────────────────────────────────────────────

    public void BeginInteract(GeneratorPuzzleInteractable interactable)
    {
        if (_currentInteractable != null && _currentInteractable != interactable)
            return;

        _currentInteractable = interactable;
        _isInteracting = true;

        if (_nextQteThreshold == 0f)
            _nextQteThreshold = GetNextQteThreshold();

        OpenUI();
        Debug.Log("[GenPuzzle] Interação iniciada.");
    }

    public void HoldInteract(GeneratorPuzzleInteractable interactable)
    {
        if (_currentInteractable != interactable) return;
        _isInteracting = true;

        // Reabre a UI se o jogador voltou a segurar
        if (!_isOpen && !_qteActive)
            OpenUI();
    }

    public void StopInteract(GeneratorPuzzleInteractable interactable)
    {
        if (_currentInteractable != interactable) return;
        _isInteracting = false;
        // A UI fecha no próximo Update (quando !_isInteracting e !_qteActive)
        Debug.Log("[GenPuzzle] Interação interrompida.");
    }

    public bool IsCurrentInteractable(GeneratorPuzzleInteractable interactable)
    {
        return _currentInteractable == interactable;
    }

    // ── QTE ───────────────────────────────────────────────────────────────────

    private void TriggerQTE()
    {
        _qteActive = true;
        _qteTimer = qteWindowSeconds;
        _isInteracting = false;

        LockPlayer(true);
        _ui?.ShowQTE(qteWindowSeconds);

        Debug.Log("[GenPuzzle] QTE disparado!");
    }

    private void ResolveQTE(bool success)
    {
        _qteActive = false;
        _ui?.HideQTE();

        if (success)
        {
            Debug.Log("[GenPuzzle] QTE bem-sucedido.");

            if (_progress >= 1f)
            {
                CompletePuzzle();
                return;
            }

            _nextQteThreshold = GetNextQteThreshold();
            _isInteracting = true;
        }
        else
        {
            Debug.Log("[GenPuzzle] QTE falhou.");

            _progress -= failRegressAmount;
            _progress = Mathf.Clamp01(_progress);

            _nextQteThreshold = GetNextQteThreshold();

            _currentInteractable?.OnPuzzleFailed();
            _isInteracting = false;
            CloseUI();
        }
    }

    // ── UI Open/Close ─────────────────────────────────────────────────────────

    private void OpenUI()
    {
        _isOpen = true;
        LockPlayer(true);
        _ui?.Show(_progress);
    }

    private void CloseUI()
    {
        _isOpen = false;
        LockPlayer(false);
        _ui?.Hide();
    }

    // ── Completar ─────────────────────────────────────────────────────────────

    private void CompletePuzzle()
    {
        _progress = 1f;
        _isInteracting = false;
        _qteActive = false;
        _isOpen = false;

        _ui?.SetProgress(1f);
        _ui?.Hide();

        _currentInteractable?.OnPuzzleCompleted();
        _currentInteractable = null;

        _progress = 0f;
        _nextQteThreshold = 0f;

        Debug.Log("[GenPuzzle] Puzzle completo!");

        // Destravar no frame seguinte evita que o Space vaze para o pulo
        StartCoroutine(UnlockNextFrame());
    }

    private System.Collections.IEnumerator UnlockNextFrame()
    {
        // Espera o Space ser solto antes de destravar
        // evita que o performed do QTE vaze para o OnJump
        float timeout = 1f;
        while (timeout > 0f)
        {
            timeout -= Time.deltaTime;
            if (Keyboard.current == null || !Keyboard.current.spaceKey.isPressed)
                break;
            yield return null;
        }

        yield return null; // 1 frame extra de segurança
        LockPlayer(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private float GetNextQteThreshold()
    {
        const float safeMax = 0.92f;

        float min = _progress + qteMinInterval;
        float max = _progress + qteMaxInterval;

        if (min >= safeMax)
            return 2f;

        max = Mathf.Min(max, safeMax);
        return Random.Range(min, max);
    }

    private void LockPlayer(bool locked)
    {
        if (_playerLocked == locked) return;
        _playerLocked = locked;

        if (playerMovement != null) playerMovement.enabled = !locked;
        if (playerLook != null) playerLook.enabled = !locked;
        if (playerInteract != null) playerInteract.enabled = !locked;

        if (playerInput != null)
        {
            var map = playerInput.actions.FindActionMap("Player");
            if (map != null)
            {
                if (locked)
                {
                    map.Disable();
                }
                else
                {
                    // Zera movimento residual antes de reativar inputs
                    if (playerMovementFull != null)
                        playerMovementFull.ResetMovementState();

                    map.Enable();
                }
            }
        }

        Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = locked;

        Debug.Log($"[GenPuzzle] Player {(locked ? "TRAVADO" : "DESTRAVADO")}.");
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!Application.isPlaying) return;
        GUI.Label(new Rect(10, 10, 300, 20), $"[GenPuzzle] Progress: {_progress:P0}");
        GUI.Label(new Rect(10, 30, 300, 20), $"[GenPuzzle] Interacting: {_isInteracting} | Open: {_isOpen} | QTE: {_qteActive}");
        GUI.Label(new Rect(10, 50, 300, 20), $"[GenPuzzle] Next QTE at: {_nextQteThreshold:P0}");
    }
#endif
}