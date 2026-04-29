using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gerencia toda a lógica do minigame de gerador estilo DBD.
/// Rode na cena de gameplay principal — NÃO carrega cena adicional.
/// 
/// Fluxo:
///   1. Jogador pressiona interagir → BeginInteract()
///   2. Enquanto segura → HoldInteract() a cada frame
///   3. Soltar → StopInteract()
///   4. QTE aparece aleatoriamente → jogador deve pressionar a tecla exibida
///   5. Sucesso = progresso continua | Falha = penalty (alerta + barulho)
///   6. Barra cheia = OnPuzzleCompleted()
/// </summary>
public class GeneratorPuzzleRuntime : MonoBehaviour
{
    // ── Configurações ──────────────────────────────────────────────────────────

    [Header("Progresso")]
    [Tooltip("Velocidade base de enchimento da barra (unidades por segundo, 0-1)")]
    public float fillSpeed = 0.06f;

    [Tooltip("Quanto regride ao errar um QTE (0-1)")]
    public float failRegressAmount = 0.15f;

    [Tooltip("Quanto regride ao soltar o gerador (por segundo)")]
    public float releaseRegressSpeed = 0.03f;

    [Header("QTE")]
    [Tooltip("Intervalo mínimo entre QTEs (segundos de progresso acumulado)")]
    public float qteMinInterval = 0.2f;

    [Tooltip("Intervalo máximo entre QTEs")]
    public float qteMaxInterval = 0.4f;

    [Tooltip("Janela de tempo que o jogador tem para reagir ao QTE (segundos)")]
    public float qteWindowSeconds = 1.2f;

    [Header("Player Lock")]
    public MonoBehaviour playerMovement;
    public MonoBehaviour playerLook;
    public PlayerInteract playerInteract;

    [Header("Input")]
    public InputActionReference interactHoldAction;   // botão segurado para progredir
    public InputActionReference qteInputAction;        // botão de QTE (ex: E ou F)

    // ── Estado interno ─────────────────────────────────────────────────────────

    private GeneratorPuzzleInteractable _currentInteractable;
    private GeneratorPuzzleUI _ui;

    private float _progress = 0f;          // 0 a 1
    private bool _isInteracting = false;
    private bool _qteActive = false;
    private float _qteTimer = 0f;
    private float _nextQteThreshold = 0f;  // progresso em que o próximo QTE dispara
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
        if (_currentInteractable == null) return;

        // ── Lógica de QTE ativo ────────────────────────────────────────────────
        if (_qteActive)
        {
            _qteTimer -= Time.deltaTime;

            _ui?.UpdateQteTimer(_qteTimer / qteWindowSeconds);

            bool qtePressed =
                (_qteInput != null && _qteInput.WasPressedThisFrame()) ||
                (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame);

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

            return; // pausa o progresso enquanto QTE está ativo
        }

        // ── Regressão ao soltar ────────────────────────────────────────────────
        if (!_isInteracting)
        {
            if (_progress > 0f)
            {
                _progress -= releaseRegressSpeed * Time.deltaTime;
                _progress = Mathf.Clamp01(_progress);
                _ui?.SetProgress(_progress);
            }
            return;
        }

        // ── Progresso enquanto segura ──────────────────────────────────────────
        _progress += fillSpeed * Time.deltaTime;
        _progress = Mathf.Clamp01(_progress);

        _ui?.SetProgress(_progress);

        // Verifica se atingiu o threshold do próximo QTE
        if (_progress >= _nextQteThreshold && _nextQteThreshold < 1f)
        {
            TriggerQTE();
            return;
        }

        // Puzzle completo
        if (_progress >= 1f)
        {
            CompletePuzzle();
        }
    }

    // ── API Pública ────────────────────────────────────────────────────────────

    /// <summary>Chamado quando o jogador inicia a interação.</summary>
    public void BeginInteract(GeneratorPuzzleInteractable interactable)
    {
        if (_currentInteractable != null && _currentInteractable != interactable)
            return; // já interagindo com outro

        _currentInteractable = interactable;
        _isInteracting = true;

        // Se é a primeira vez, inicializa threshold do QTE
        if (_nextQteThreshold == 0f)
            _nextQteThreshold = GetNextQteThreshold();

        LockPlayer(true);
        _ui?.Show(_progress);

        Debug.Log("[GenPuzzle] Interação iniciada.");
    }

    /// <summary>Chamado a cada frame enquanto o jogador segura.</summary>
    public void HoldInteract(GeneratorPuzzleInteractable interactable)
    {
        if (_currentInteractable != interactable) return;
        _isInteracting = true;
    }

    /// <summary>Chamado quando o jogador solta.</summary>
    public void StopInteract(GeneratorPuzzleInteractable interactable)
    {
        if (_currentInteractable != interactable) return;
        _isInteracting = false;

        if (!_qteActive)
            LockPlayer(false);

        Debug.Log("[GenPuzzle] Interação interrompida.");
    }

    // ── QTE ───────────────────────────────────────────────────────────────────

    private void TriggerQTE()
    {
        _qteActive = true;
        _qteTimer = qteWindowSeconds;
        _isInteracting = false; // congela progresso

        // Garante que o player está travado durante o QTE
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

            // Se o progresso já estava em 100% quando o QTE foi resolvido,
            // conclui imediatamente em vez de reagendar outro QTE.
            if (_progress >= 1f)
            {
                CompletePuzzle();
                return;
            }

            _nextQteThreshold = GetNextQteThreshold();
            _isInteracting = true; // retoma progresso
        }
        else
        {
            Debug.Log("[GenPuzzle] QTE falhou.");

            _progress -= failRegressAmount;
            _progress = Mathf.Clamp01(_progress);
            _ui?.SetProgress(_progress);

            // Recalcula threshold pois o progresso regrediu
            _nextQteThreshold = GetNextQteThreshold();

            _currentInteractable.OnPuzzleFailed();
            _isInteracting = false;
            LockPlayer(false);
        }
    }

    // ── Completar ─────────────────────────────────────────────────────────────

    private void CompletePuzzle()
    {
        _progress = 1f;
        _isInteracting = false;
        _qteActive = false;

        _ui?.SetProgress(1f);
        _ui?.Hide();

        LockPlayer(false);

        _currentInteractable.OnPuzzleCompleted();
        _currentInteractable = null;

        // Reseta para próximo uso (outro gerador)
        _progress = 0f;
        _nextQteThreshold = 0f;

        Debug.Log("[GenPuzzle] Puzzle completo!");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private float GetNextQteThreshold()
    {
        // Margem de segurança: nunca agenda QTE no trecho final da barra,
        // evitando loop infinito quando o progresso está próximo de 1.
        const float safeMax = 0.92f;

        float min = _progress + qteMinInterval;
        float max = _progress + qteMaxInterval;

        // Se até o mínimo já está fora da zona segura, não haverá mais QTEs —
        // retorna 2f (nunca atingido) para a barra completar livremente.
        if (min >= safeMax)
            return 2f;

        max = Mathf.Min(max, safeMax);
        return Random.Range(min, max);
    }

    private void LockPlayer(bool locked)
    {
        if (_playerLocked == locked) return;
        _playerLocked = locked;

        if (playerMovement != null)
            playerMovement.enabled = !locked;

        if (playerLook != null)
            playerLook.enabled = !locked;

        if (playerInteract != null)
            playerInteract.enabled = !locked;

        Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = locked;

        Debug.Log($"[GenPuzzle] Player {(locked ? "TRAVADO" : "DESTRAVADO")}.");
    }

    /// <summary>Retorna true se o interactable passado é o que está ativo no momento.</summary>
    public bool IsCurrentInteractable(GeneratorPuzzleInteractable interactable)
    {
        return _currentInteractable == interactable;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!Application.isPlaying) return;

        GUI.Label(new Rect(10, 10, 300, 20), $"[GenPuzzle] Progress: {_progress:P0}");
        GUI.Label(new Rect(10, 30, 300, 20), $"[GenPuzzle] Interacting: {_isInteracting} | QTE: {_qteActive}");
        GUI.Label(new Rect(10, 50, 300, 20), $"[GenPuzzle] Next QTE at: {_nextQteThreshold:P0}");
    }
#endif
}