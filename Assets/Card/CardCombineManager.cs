using System.Collections.Generic;
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

    [Header("조합 미리보기 UI 참조")]
    public CardCraftPreview craftPreview;

    private void Update()
    {
        UpdateCraftPreview();
    }

    public bool HasCardsInCombineSlots()
    {
        CardUI card1 = combineSlot1 != null ? combineSlot1.GetComponentInChildren<CardUI>() : null;
        CardUI card2 = combineSlot2 != null ? combineSlot2.GetComponentInChildren<CardUI>() : null;

        return (card1 != null || card2 != null);
    }

    public void UpdateCraftPreview()
    {
        if (craftPreview == null) return;

        CardUI card1 = combineSlot1 != null ? combineSlot1.GetComponentInChildren<CardUI>() : null;
        CardUI card2 = combineSlot2 != null ? combineSlot2.GetComponentInChildren<CardUI>() : null;

        if (card1 != null && card1.cardData != null && card2 != null && card2.cardData != null && recipeDatabase != null)
        {
            CardData resultData = recipeDatabase.GetRecipeResult(card1.cardData, card2.cardData);

            if (resultData != null && IsCardUnlocked(resultData.cardName))
            {
                craftPreview.ShowPreview(resultData);
            }
            else
            {
                craftPreview.HidePreview();
            }
        }
        else
        {
            craftPreview.HidePreview();
        }
    }

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

        if (resultData == null)
        {
            Debug.Log("[조합 실패] 등록되어 있지 않은 조합식입니다.");
            return;
        }

        if (!IsCardUnlocked(resultData.cardName))
        {
            Debug.LogWarning($"[조합 불가] 결과 카드 [{resultData.cardName}]를 상점이나 이벤트에서 먼저 구매/획득해야 조합할 수 있습니다!");
            return;
        }

        Debug.Log($"[조합 성공] {card1.cardData.cardName} + {card2.cardData.cardName} => {resultData.cardName}");

        // ★ [연금술의 황금 저울] 카드를 2장 조합할 때마다 즉시 15골드 획득
        if (RunState.Instance != null && RunState.Instance.HasRelicEffect(StatBonusType.AlchemyScale))
        {
            RunState.Instance.gold += 15;
            Debug.Log($"[연금술의 황금 저울] 카드 조합 성공! 즉시 +15 골드 획득 (현재 골드: {RunState.Instance.gold})");
        }

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

        if (craftPreview != null)
        {
            craftPreview.HidePreview();
        }
    }

    private bool IsCardUnlocked(string cardName)
    {
        return EncyclopediaManager.Instance != null && EncyclopediaManager.Instance.IsUnlocked(cardName);
    }
}