using UnityEngine;
using UnityEngine.UI; // ★ 이 부분이 꼭 있어야 Image 타입을 쓸 수 있습니다!
using UnityEngine.EventSystems;
using TMPro;

public class CardUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Data & Manager")]
    public CardData cardData;
    private TurnBasedGameManager gameManager;

    [Header("UI Components")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI valueText;
    public TextMeshProUGUI typeText;

    // ★ 이 줄이 빠져있어서 인스펙터에 안 나왔던 것입니다!
    public Image cardImageComponent;

    [Header("Selection Visual")]
    public bool isSelected = false;
    private Vector3 originalPosition;
    public float selectOffsetY = 30f;

    private void Start()
    {
        gameManager = FindObjectOfType<TurnBasedGameManager>();
        originalPosition = transform.localPosition;
    }

    public void Setup(CardData data)
    {
        cardData = data;
        if (nameText != null) nameText.text = data.cardName;
        if (valueText != null) valueText.text = data.value.ToString();
        if (typeText != null) typeText.text = data.cardType.ToString();

        // ★ CardData의 이미지를 UI Image에 적용
        if (cardImageComponent != null && data.cardIcon != null)
        {
            cardImageComponent.sprite = data.cardIcon;
        }
    }

    // 카드를 클릭했을 때 실행되는 함수 (IPointerClickHandler)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (gameManager == null || cardData == null) return;

        // 이미 3장이 선택되었고, 현재 카드가 선택 안 된 상태라면 클릭 무시
        if (!isSelected && gameManager.selectedCards.Count >= 3)
        {
            Debug.Log("이미 3장의 카드를 모두 선택했습니다!");
            return;
        }

        // 토글 방식 (선택 <-> 해제)
        isSelected = !isSelected;

        if (isSelected)
        {
            // GameManager 선택 리스트에 추가
            gameManager.SelectCard(cardData);
            // 시각 효과: 카드가 위로 살짝 올라감
            transform.localPosition = originalPosition + new Vector3(0, selectOffsetY, 0);
        }
        else
        {
            // GameManager 선택 리스트에서 제거
            gameManager.selectedCards.Remove(cardData);
            // 시각 효과: 원래 위치로 복귀
            transform.localPosition = originalPosition;
        }
    }

    // 턴이 끝난 후 선택 상태 초기화
    public void ResetSelection()
    {
        isSelected = false;
        transform.localPosition = originalPosition;
    }
}