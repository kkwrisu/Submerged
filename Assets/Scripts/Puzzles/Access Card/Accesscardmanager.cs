using UnityEngine;
using UnityEngine.Events;
public class AccessCardManager : MonoBehaviour
{
    public static AccessCardManager Instance { get; private set; }

    [Header("Estado")]
    [SerializeField] private int _cardLevel = 1;

    [Header("Eventos")]
    public UnityEvent<int> onCardLevelChanged; // dispara com o novo nível

    public int CardLevel => _cardLevel;

    // ── Unity ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── API Pública ────────────────────────────────────────────────────────────

    /// <summary>
    /// Aumenta o nível do cartão em +1 e salva automaticamente.
    /// Chamado pelo ComputerHackInteractable ao completar o puzzle.
    /// </summary>
    public void UpgradeCard()
    {
        _cardLevel++;
        Debug.Log($"[AccessCard] Nível atualizado para {_cardLevel}.");
        onCardLevelChanged?.Invoke(_cardLevel);

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();
    }

    /// <summary>
    /// Define o nível diretamente (usado pelo sistema de save ao carregar).
    /// </summary>
    public void SetLevel(int level)
    {
        _cardLevel = Mathf.Max(1, level);
        onCardLevelChanged?.Invoke(_cardLevel);
    }

    /// <summary>
    /// Retorna true se o jogador tem nível suficiente para o leitor pedido.
    /// </summary>
    public bool HasAccess(int requiredLevel)
    {
        return _cardLevel >= requiredLevel;
    }
}