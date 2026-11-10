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
        // 실시간으로 조합 슬롯의 카드 상태를 확인하여 미리보기 갱신
        UpdateCraftPreview();
    }

    /// <summary>
    /// 조합 슬롯 2곳 중 카드가 1장이라도 올라와 있는지 확인합니다.
    /// </summary>
    public bool HasCardsInCombineSlots()
    {
        CardUI card1 = combineSlot1 != null ? combineSlot1.GetComponentInChildren<CardUI>() : null;
        CardUI card2 = combineSlot2 != null ? combineSlot2.GetComponentInChildren<CardUI>() : null;

        return (card1 != null || card2 != null);
    }

    /// <summary>
    /// 조합 슬롯 2곳의 카드를 확인하여 미리보기 UI를 업데이트합니다.
    /// (결과 카드가 상점/이벤트 등에서 이미 해금(구매)된 경우에만 미리보기가 표시됩니다.)
    /// </summary>
    public void UpdateCraftPreview()
    {
        if (craftPreview == null) return;

        CardUI card1 = combineSlot1 != null ? combineSlot1.GetComponentInChildren<CardUI>() : null;
        CardUI card2 = combineSlot2 != null ? combineSlot2.GetComponentInChildren<CardUI>() : null;

        if (card1 != null && card1.cardData != null && card2 != null && card2.cardData != null && recipeDatabase != null)
        {
            CardData resultData = recipeDatabase.GetRecipeResult(card1.cardData, card2.cardData);

            // ★ [핵심] 결과물 카드가 해금(구매)되어 있는지 도감 매니저에서 확인
            if (resultData != null && IsCardUnlocked(resultData.cardName))
            {
                craftPreview.ShowPreview(resultData);
            }
            else
            {
                // 아직 구매/해금하지 않은 카드라면 미리보기 숨김
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

        // ★ [핵심] 상점/이벤트에서 결과 카드를 먼저 구매/해금했는지 확인
        if (!IsCardUnlocked(resultData.cardName))
        {
            Debug.LogWarning($"[조합 불가] 결과 카드 [{resultData.cardName}]를 상점이나 이벤트에서 먼저 구매/획득해야 조합할 수 있습니다!");
            return;
        }

        // 조합 성공 처리
        Debug.Log($"[조합 성공] {card1.cardData.cardName} + {card2.cardData.cardName} => {resultData.cardName}");

        // 사용한 재료 카드 파괴
        Destroy(card1.gameObject);
        Destroy(card2.gameObject);

        // 결과 카드를 손패 생성
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

    /// <summary>
    /// 카드가 해금(구매)된 상태인지 확인하는 헬퍼 메서드
    /// </summary>
    private bool IsCardUnlocked(string cardName)
    {
        return EncyclopediaManager.Instance != null && EncyclopediaManager.Instance.IsUnlocked(cardName);
    }
}