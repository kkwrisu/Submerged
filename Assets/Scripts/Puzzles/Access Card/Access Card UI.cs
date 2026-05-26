using UnityEngine;
using UnityEngine.UI;

public class AccessCardUI : MonoBehaviour
{
    [Header("Imagens por nível (índice 0 = nível 1)")]
    [SerializeField] private Sprite[] cardSprites;

    [Header("Referência")]
    [SerializeField] private Image cardImage;

    private void Start()
    {
        if (AccessCardManager.Instance != null)
        {
            AccessCardManager.Instance.onCardLevelChanged.AddListener(UpdateCardImage);
            UpdateCardImage(AccessCardManager.Instance.CardLevel);
        }
    }

    private void OnDestroy()
    {
        if (AccessCardManager.Instance != null)
            AccessCardManager.Instance.onCardLevelChanged.RemoveListener(UpdateCardImage);
    }

    private void UpdateCardImage(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, cardSprites.Length - 1);
        cardImage.sprite = cardSprites[index];
    }
}