using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardBookUI : MonoBehaviour
{
    [Header("도감 팝업 패널")]
    public GameObject bookPanel;

    [Header("카드 데이터베이스 & 그리드")]
    [Tooltip("도감에 등록될 전체 카드 목록입니다. 새로 만든 카드 에셋을 여기에 추가하세요.")]
    public List<CardData> allCards;
    public Transform gridContainer;
    public EncyclopediaSlotUI slotPrefab;

    [Header("카드 상세 정보 팝업 UI (Card Detail Modal)")]
    public GameObject detailModalPanel;            // 상세 창 전체 패널
    public Image detailCardArtImage;               // 카드 일러스트 이미지
    public TextMeshProUGUI detailCardNameText;     // 카드 이름
    public TextMeshProUGUI detailCardTypeText;     // 카드 타입
    public TextMeshProUGUI detailCardDescText;     // 카드 효과 설명
    public TextMeshProUGUI detailCardStatsText;    // 카드 스탯 (공격력/체력/실드/버프 등)
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

            // CS7036 해결 포인트: 두 번째 인자로 discussion(this) 전달
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

        // ★ [스탯 출력 로직 개선] 카드가 가진 스탯(공격/체력/실드/디버프/버프)에 따라 동적으로 텍스트 생성
        if (detailCardStatsText != null)
        {
            List<string> statStrings = new List<string>();

            // 1. 공격력
            int attackVal = card.attack > 0 ? card.attack : (card.cardType == CardType.Attack ? card.value : 0);
            if (attackVal > 0) statStrings.Add($"공격력: {attackVal}");

            // 2. 체력 / 힐
            int healthVal = card.health > 0 ? card.health : (card.cardType == CardType.Heal ? card.value : 0);
            if (healthVal > 0) statStrings.Add($"회복량: {healthVal}");

            // 3. 실드 / 배리어
            int shieldVal = card.shield > 0 ? card.shield : card.shieldValue;
            if (shieldVal > 0) statStrings.Add($"실드: {shieldVal}");

            // 4. 디버프
            int debuffVal = card.debuff > 0 ? card.debuff : (card.cardType == CardType.Debuff ? card.value : 0);
            if (debuffVal > 0) statStrings.Add($"디버프: {debuffVal}");

            // 5. 버프
            int buffVal = card.buff > 0 ? card.buff : card.buffValue;
            if (buffVal > 0) statStrings.Add($"공격 버프: +{buffVal}");

            if (statStrings.Count > 0)
            {
                detailCardStatsText.text = string.Join(" | ", statStrings);
            }
            else
            {
                detailCardStatsText.text = "";
            }
        }

        detailModalPanel.SetActive(true);
    }

    public void CloseCardDetail()
    {
        if (detailModalPanel != null)
            detailModalPanel.SetActive(false);
    }
}