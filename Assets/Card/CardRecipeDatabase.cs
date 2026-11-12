using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct CardRecipe
{
    public CardData cardA;      // 재료 카드 1
    public CardData cardB;      // 재료 카드 2
    public CardData resultCard; // 조합 결과 카드
}

[CreateAssetMenu(fileName = "CardRecipeDatabase", menuName = "Card System/Recipe Database")]
public class CardRecipeDatabase : ScriptableObject
{
    public List<CardRecipe> recipes = new List<CardRecipe>();

    public CardData GetRecipeResult(CardData a, CardData b)
    {
        if (a == null || b == null) return null;

        // ★ 강화 카드(파이어볼+2)도 원래 카드(파이어볼)로 조합에 쓸 수 있게
        a = a.BaseCard;
        b = b.BaseCard;

        foreach (var recipe in recipes)
        {
            if ((recipe.cardA == a && recipe.cardB == b) || (recipe.cardA == b && recipe.cardB == a))
            {
                return recipe.resultCard;
            }
        }

        return null;
    }
}
