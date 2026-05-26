using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class AccessCardManager : MonoBehaviour
{
    public static AccessCardManager Instance { get; private set; }

    [Header("Estado")]
    [SerializeField] private int _cardLevel = 1;

    [Header("Eventos")]
    public UnityEvent<int> onCardLevelChanged;

    public int CardLevel => _cardLevel;

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

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.digit7Key.wasPressedThisFrame)
            UpgradeCard();
    }

    public void UpgradeCard()
    {
        _cardLevel++;
        Debug.Log($"[AccessCard] Nível atualizado para {_cardLevel}.");
        onCardLevelChanged?.Invoke(_cardLevel);

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();
    }

    public void SetLevel(int level)
    {
        _cardLevel = Mathf.Max(1, level);
        onCardLevelChanged?.Invoke(_cardLevel);
    }

    public bool HasAccess(int requiredLevel)
    {
        return _cardLevel >= requiredLevel;
    }
}