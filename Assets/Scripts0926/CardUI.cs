using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용 시 (일반 Text 사용 시 Text로 변경)

public class CardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    public Image cardImage;
    public TextMeshProUGUI cardNameText; // 일반 Text는 'public Text cardNameText;'

    [HideInInspector] public CardData cardData; // 현재 카드의 데이터 저장
    [HideInInspector] public Transform parentToReturnTo = null;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    private void Awake()
    {
        // CanvasGroup이 붙어있지 않다면 코드로 자동 추가하여 에러 방지
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        rectTransform = GetComponent<RectTransform>();
        if (cardImage == null) cardImage = GetComponent<Image>();
    }

    // ScriptableObject 데이터를 UI에 바인딩
    public void SetupCard(CardData data)
    {
        if (data == null) return;

        this.cardData = data;

        if (cardNameText != null)
            cardNameText.text = data.cardName;

        if (cardImage != null)
        {
            if (data.cardIcon != null)
            {
                cardImage.sprite = data.cardIcon;
                cardImage.color = Color.white; // 원본 이미지 색상 노출
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        parentToReturnTo = this.transform.parent;

        // 드래그 중 최상위 Canvas로 이동하여 레이어 가림 방지
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            this.transform.SetParent(canvas.transform, true);
        }

        canvasGroup.blocksRaycasts = false; // 드롭 위치(Slot/Hand) 레이캐스트 감지 허용
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPointerPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            this.transform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPointerPos))
        {
            rectTransform.localPosition = localPointerPos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // 드롭 구역(슬롯 또는 HandPanel)으로 부모 설정
        this.transform.SetParent(parentToReturnTo);

        // [핵심] 부모의 중앙(0,0,0)에 오도록 위치 및 크기 강제 정렬
        this.transform.localScale = Vector3.one;

        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = Vector2.zero; // 자식으로서 중앙 정렬
            rect.anchoredPosition3D = Vector3.zero;
        }
    }
}