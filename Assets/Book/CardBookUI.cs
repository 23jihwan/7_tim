using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardBookUI : MonoBehaviour
{
    [Header("도감 팝업 패널")]
    public GameObject bookPanel;

    [Header("카드 데이터베이스 & 그리드")]
    public List<CardData> allCards;
    public Transform gridContainer;
    public EncyclopediaSlotUI slotPrefab;

    [Header("카드 상세 정보 팝업 UI (Card Detail Modal)")]
    public GameObject detailModalPanel;            // 상세 창 전체 패널
    public Image detailCardArtImage;               // 카드 일러스트 이미지
    public TextMeshProUGUI detailCardNameText;     // 카드 이름
    public TextMeshProUGUI detailCardTypeText;     // 카드 타입
    public TextMeshProUGUI detailCardDescText;     // 카드 효과 설명
    public TextMeshProUGUI detailCardStatsText;    // 카드 스탯 (공격력/체력)
    public Button detailCloseButton;               // 팝업 닫기 버튼

    private void Start()
    {
        if (bookPanel != null) bookPanel.SetActive(false);
        if (detailModalPanel != null) detailModalPanel.SetActive(false);

        if (detailCloseButton != null)
            detailCloseButton.onClick.AddListener(CloseCardDetail);
    }

    public void OpenBook()
    {
        if (bookPanel != null)
        {
            bookPanel.SetActive(true);
            RefreshBook();
        }
    }

    public void CloseBook()
    {
        if (bookPanel != null) bookPanel.SetActive(false);
        CloseCardDetail();
    }

    public void RefreshBook()
    {
        if (!Application.isPlaying) return;
        if (gridContainer == null || slotPrefab == null) return;

        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }

        foreach (Transform child in gridContainer)
        {
            if (child != null && child.gameObject != null)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        foreach (CardData card in allCards)
        {
            if (card == null) continue;
            EncyclopediaSlotUI slot = Instantiate(slotPrefab, gridContainer);

            // ★ CS7036 해결 포인트: 두 번째 인자로 this 전달
            slot.Setup(card, this);
        }
    }

    public void ShowCardDetail(CardData card)
    {
        if (card == null || detailModalPanel == null) return;

        if (detailCardArtImage != null) detailCardArtImage.sprite = card.cardIcon;
        if (detailCardNameText != null) detailCardNameText.text = card.cardName;
        if (detailCardTypeText != null) detailCardTypeText.text = $"TYPE: {card.cardType}";
        if (detailCardDescText != null) detailCardDescText.text = card.description;
        if (detailCardStatsText != null) detailCardStatsText.text = $"{card.attack}/{card.health}";

        detailModalPanel.SetActive(true);
    }

    public void CloseCardDetail()
    {
        if (detailModalPanel != null)
            detailModalPanel.SetActive(false);
    }
}