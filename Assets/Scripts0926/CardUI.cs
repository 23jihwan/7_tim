using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CardUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
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

        // 4. 카드 설명 (자동 생성 및 예외 처리)
        if (descriptionText != null)
        {
            // CardData에 설명글이 수동으로 써있다면 그대로 출력
            if (!string.IsNullOrEmpty(cardData.description))
            {
                descriptionText.text = cardData.description;
            }
            else
            {
                // 설명글이 없으면 속성과 수치를 조합해 자동으로 생성 (예: "파이어 10")
                descriptionText.text = $"{cardData.cardType} {cardData.value}";
            }
        }

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
        transform.SetParent(parentToReturnTo);
        canvasGroup.blocksRaycasts = true;
    }
}