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