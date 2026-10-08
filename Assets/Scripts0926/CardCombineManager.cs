using UnityEngine;

public class CardCombineManager : MonoBehaviour
{
    [Header("조합 UI 슬롯 참조")]
    public CombineSlot combineSlot1;
    public CombineSlot combineSlot2;

    [Header("손패 및 프리팹 참조")]
    public Transform handPanel;               // 손패(HandPanel) Transform
    public GameObject cardPrefab;             // 카드 프리팹
    public CardRecipeDatabase recipeDatabase; // 레시피 데이터베이스 에셋

    // [조합하기] 버튼 OnClick()에 연결할 함수
    public void OnCombineButtonClicked()
    {
        if (combineSlot1 == null || combineSlot2 == null)
        {
            Debug.LogError("[CombineManager] 조합 슬롯이 연결되지 않았습니다!");
            return;
        }

        // 1. 두 슬롯에서 CardUI 가져오기
        CardUI card1 = combineSlot1.GetComponentInChildren<CardUI>();
        CardUI card2 = combineSlot2.GetComponentInChildren<CardUI>();

        // 두 슬롯 중 하나라도 비어있는 경우
        if (card1 == null || card2 == null)
        {
            Debug.Log("[조합 실패] 조합 슬롯 2곳에 카드를 모두 올려주세요.");
            return;
        }

        // 2. 타입 검사 (안전장치)
        if (card1.cardData.cardType != card2.cardData.cardType)
        {
            Debug.LogWarning("[조합 실패] 서로 다른 타입의 카드는 조합할 수 없습니다.");
            return;
        }

        // 3. 레시피 데이터베이스 존재 여부 확인
        if (recipeDatabase == null)
        {
            Debug.LogError("[CombineManager] RecipeDatabase 에셋이 연결되지 않았습니다.");
            return;
        }

        // 4. 레시피 검색
        CardData resultData = recipeDatabase.GetRecipeResult(card1.cardData, card2.cardData);

        if (resultData != null)
        {
            Debug.Log($"[조합 성공] {card1.cardData.cardName} + {card2.cardData.cardName} => {resultData.cardName}");

            // 5. 조합에 사용된 슬롯 내부의 카드 2장 파괴 (손패/슬롯에서 삭제)
            Destroy(card1.gameObject);
            Destroy(card2.gameObject);

            // 6. 완성된 카드를 손패(handPanel)의 자식으로 생성
            if (cardPrefab != null && handPanel != null)
            {
                GameObject newCardObj = Instantiate(cardPrefab, handPanel);
                CardUI newCardUI = newCardObj.GetComponent<CardUI>();

                if (newCardUI != null)
                {
                    // CardUI의 초기화 메서드 호출 (SetupCard 또는 Setup)
                    newCardUI.SetupCard(resultData);
                }
            }
        }
        else
        {
            Debug.Log("[조합 실패] 존재하지 않는 카드 조합식입니다.");
        }
    }
}