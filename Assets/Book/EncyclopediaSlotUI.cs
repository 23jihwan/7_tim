using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EncyclopediaSlotUI : MonoBehaviour
{
    [Header("UI 컴포넌트 연결")]
    public Image cardIconImage;
    public TextMeshProUGUI cardNameText;
    public Image lockOverlay;
    public Button slotButton;

    [Header("잠금 상태 기본 설정")]
    public Sprite lockedIconSprite;

    private CardData currentCard;
    private bool isUnlocked;

    public void Setup(CardData card, CardBookUI bookUI)
    {
        if (card == null) return;

        currentCard = card;
        isUnlocked = EncyclopediaManager.Instance != null && EncyclopediaManager.Instance.IsUnlocked(card.cardName);

        if (isUnlocked)
        {
            if (cardIconImage != null)
            {
                cardIconImage.sprite = card.cardIcon;
                cardIconImage.color = Color.white;
            }
            if (cardNameText != null) cardNameText.text = card.cardName;
            if (lockOverlay != null) lockOverlay.gameObject.SetActive(false);
        }
        else
        {
            if (cardIconImage != null)
            {
                if (lockedIconSprite != null)
                    cardIconImage.sprite = lockedIconSprite;
                else
                    cardIconImage.color = Color.black;
            }
            if (cardNameText != null) cardNameText.text = "???";
            if (lockOverlay != null) lockOverlay.gameObject.SetActive(true);
        }

        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(() =>
            {
                if (isUnlocked && bookUI != null)
                {
                    bookUI.ShowCardDetail(currentCard);
                }
            });
        }
    }
}