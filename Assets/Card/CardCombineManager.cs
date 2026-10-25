using UnityEngine;

public class CardCombineManager : MonoBehaviour
{
    [Header("조합 UI 슬롯 참조")]
    public CombineSlot combineSlot1;
    public CombineSlot combineSlot2;

    [Header("손패 및 프리팹 참조")]
    public Transform handPanel;
    public GameObject cardPrefab;
    public CardRecipeDatabase recipeDatabase;

    public void OnCombineButtonClicked()
    {
        if (combineSlot1 == null || combineSlot2 == null)
        {
            Debug.LogError("[CombineManager] 조합 슬롯이 연결되지 않았습니다!");
            return;
        }

        CardUI card1 = combineSlot1.GetComponentInChildren<CardUI>();
        CardUI card2 = combineSlot2.GetComponentInChildren<CardUI>();

        if (card1 == null || card2 == null)
        {
            Debug.Log("[조합 실패] 조합 슬롯 2곳에 카드를 모두 올려주세요.");
            return;
        }

        if (recipeDatabase == null)
        {
            Debug.LogError("[CombineManager] RecipeDatabase 에셋이 연결되지 않았습니다.");
            return;
        }

        CardData resultData = recipeDatabase.GetRecipeResult(card1.cardData, card2.cardData);

        if (resultData != null)
        {
            Debug.Log($"[조합 성공] {card1.cardData.cardName} + {card2.cardData.cardName} => {resultData.cardName}");

            Destroy(card1.gameObject);
            Destroy(card2.gameObject);

            if (cardPrefab != null && handPanel != null)
            {
                GameObject newCardObj = Instantiate(cardPrefab, handPanel);
                CardUI newCardUI = newCardObj.GetComponent<CardUI>();

                if (newCardUI != null)
                {
                    newCardUI.SetupCard(resultData);
                }
            }
        }
        else
        {
            Debug.Log("[조합 실패] 등록되어 있지 않은 조합식입니다.");
        }
    }
}