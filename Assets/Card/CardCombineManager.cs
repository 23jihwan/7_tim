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
    public CardCraftPreview craftPreview; // 💡 미리보기 스크립트 연결 칸

    private void Update()
    {
        // 실시간으로 조합 슬롯의 카드 상태를 확인하여 미리보기 갱신
        UpdateCraftPreview();
    }

    /// <summary>
    /// 조합 슬롯 2곳의 카드를 확인하여 미리보기 UI를 업데이트합니다.
    /// </summary>
    public void UpdateCraftPreview()
    {
        if (craftPreview == null) return;

        // 슬롯에서 CardUI 컴포넌트 가져오기
        CardUI card1 = combineSlot1 != null ? combineSlot1.GetComponentInChildren<CardUI>() : null;
        CardUI card2 = combineSlot2 != null ? combineSlot2.GetComponentInChildren<CardUI>() : null;

        // 두 슬롯 모두 카드가 올라와 있고 레시피 DB가 연결된 경우
        if (card1 != null && card1.cardData != null && card2 != null && card2.cardData != null && recipeDatabase != null)
        {
            CardData resultData = recipeDatabase.GetRecipeResult(card1.cardData, card2.cardData);
            craftPreview.ShowPreview(resultData);
        }
        else
        {
            // 카드가 없거나 조합 조건을 충족하지 못하면 숨김
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

        if (resultData != null)
        {
            Debug.Log($"[조합 성공] {card1.cardData.cardName} + {card2.cardData.cardName} => {resultData.cardName}");

            // ★ [추가] 조합으로 새로 조합해낸 결과 카드를 도감에 해금!
            if (EncyclopediaManager.Instance != null)
            {
                EncyclopediaManager.Instance.UnlockCard(resultData.cardName);
            }

            // ★ 조합법(재료 → 결과)도 해금 기록 (덱에 마우스를 올리면 보이는 조합법 창)
            RecipeBook.Unlock(card1.cardData, card2.cardData);

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

            // 조합 완료 후 미리보기 숨김
            if (craftPreview != null)
            {
                craftPreview.HidePreview();
            }
        }
        else
        {
            Debug.Log("[조합 실패] 등록되어 있지 않은 조합식입니다.");
        }
    }
}