using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct CardRecipe
{
    public CardData cardA;      // 재료 카드 1
    public CardData cardB;      // 재료 카드 2
    public CardData resultCard; // 결과 카드
}

[CreateAssetMenu(fileName = "CardRecipeDatabase", menuName = "Card System/Recipe Database")]
public class CardRecipeDatabase : ScriptableObject
{
    public List<CardRecipe> recipes = new List<CardRecipe>();

    // 두 카드를 넣으면 조합 결과 카드를 반환하는 함수
    public CardData GetRecipeResult(CardData a, CardData b)
    {
        if (a == null || b == null) return null;

        // [안전장치] 타입이 다르면(예: 공격 + 힐) 조합 불가
        if (a.cardType != b.cardType)
        {
            Debug.LogWarning($"[조합 불가] {a.cardType} 카드와 {b.cardType} 카드는 조합할 수 없습니다.");
            return null;
        }

        // 순서에 상관없이 (A+B) 또는 (B+A) 조합식 검색
        foreach (var recipe in recipes)
        {
            if ((recipe.cardA == a && recipe.cardB == b) || (recipe.cardA == b && recipe.cardB == a))
            {
                return recipe.resultCard;
            }
        }

        return null; // 조합식이 없는 경우
    }
}