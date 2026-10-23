using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CardUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Card Data Reference")]
    public CardData cardData;

    [Header("Drag & Drop / Hand System")]
    public Transform parentToReturnTo;

    [Header("UI Components (선택 사항)")]
    public TMP_Text cardNameText;
    public TMP_Text cardTypeText;    // 연결하지 않아도(None) 에러 없음
    public TMP_Text valueText;       // 연결하지 않고(None) 비워둬도 에러 없음
    public TMP_Text descriptionText;
    public Image cardIconImage;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        if (cardData != null)
        {
            SetupCard(cardData);
        }
    }

    // GameManager, CardCombineManager 호환용
    public void SetupCard(CardData data)
    {
        SetupCardUI(data);
    }

    public void SetupCardUI(CardData data)
    {
        cardData = data;
        if (cardData == null) return;

        // 1. 카드 이름
        if (cardNameText != null)
            cardNameText.text = cardData.cardName;

        // 2. 카드 속성 / 타입 (UI가 있을 경우만)
        if (cardTypeText != null)
            cardTypeText.text = cardData.cardType.ToString();

        // 3. 카드 수치 (UI가 있을 경우만)
        if (valueText != null)
            valueText.text = cardData.value.ToString();

        // 4. 카드 설명 (CardData에 직접 쓴 설명이 있으면 그대로, 없으면 자동으로 만든다)
        if (descriptionText != null)
            descriptionText.text = GetDescription(cardData);

        // 5. 카드 일러스트 아이콘
        if (cardIconImage != null && cardData.cardIcon != null)
        {
            cardIconImage.sprite = cardData.cardIcon;
            cardIconImage.gameObject.SetActive(true);
        }
    }

    // --- 마우스 클릭 & 드래그 이벤트 ---
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[카드 클릭 성공] 클릭한 카드: {cardData?.cardName}");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        CardHoverPreview.Hide();          // 카드를 잡으면 확대 미리보기 숨기기
        CardAnimator.ClearHover(this);    // 떠오른 상태 지우기

        parentToReturnTo = transform.parent;

        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
        {
            transform.SetParent(rootCanvas.transform);
        }

        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        Vector3 dropPosition = this.transform.position;   // 손을 놓은 위치 기억

        // 드롭 구역(슬롯 또는 HandPanel)으로 부모 설정
        this.transform.SetParent(parentToReturnTo);

        // 부모의 중앙에 오도록 위치 및 크기 정렬
        this.transform.localScale = Vector3.one;

        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = Vector2.zero;
            rect.anchoredPosition3D = Vector3.zero;
        }

        // 놓은 자리에서 부드럽게 이동. 손패가 아니면(슬롯이면) '탁' 튕기기
        bool intoHand = parentToReturnTo != null && parentToReturnTo.GetComponent<HandPanelZone>() != null;
        CardAnimator.PlaySettle(this, dropPosition, !intoHand);
    }

    // --- 마우스를 올리면: 크게 보여주기 + 손패에서 살짝 떠오르기 ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.dragging) return;   // 다른 카드를 끌고 있을 때는 무시
        CardHoverPreview.Show(this);
        CardAnimator.SetHover(this, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CardHoverPreview.Hide(this);
        CardAnimator.SetHover(this, false);
    }

    private void OnDisable()
    {
        // 카드를 사용해서 사라질 때 미리보기도 같이 지운다
        CardHoverPreview.Hide(this);
    }

    // --- 효과 설명 자동 만들기 ---
    public static string GetDescription(CardData data)
    {
        if (!string.IsNullOrEmpty(data.description)) return data.description;

        switch (data.cardType)
        {
            case CardType.Attack:
                return $"적에게 {data.value} 피해를 준다.";
            case CardType.Heal:
                string text = $"체력을 {data.value} 회복한다.";
                if (data.damageValue > 0) text += $"\n적에게 {data.damageValue} 피해를 준다.";
                return text;
            case CardType.Debuff:
                return $"이번 턴 적의 공격력을 {data.value} 낮춘다.";
        }
        return $"{data.cardType} {data.value}";
    }
}