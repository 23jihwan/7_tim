using System.Collections.Generic;
using UnityEngine;

// 조합법 얻기 (엘리트 · 보스 · 이벤트 보상)
// 우리 게임 규칙: "결과 카드"가 도감에 해금되어 있어야 그 조합을 할 수 있다 (CardCombineManager)
// 그래서 조합법을 얻는다 = 결과 카드를 도감에 해금한다
public static class RecipeUnlock
{
    // ---------------- 단계 계산 ----------------
    // 1단계: 재료가 모두 기본 카드 / 2단계: 재료 중에 1단계 결과가 있음 / 3단계: 2단계 결과가 있음

    public static int GetTier(CardRecipeDatabase db, CardRecipe recipe)
    {
        return 1 + Mathf.Max(CardTier(db, recipe.cardA, 0), CardTier(db, recipe.cardB, 0));
    }

    private static int CardTier(CardRecipeDatabase db, CardData card, int depth)
    {
        if (card == null || depth > 10) return 0;   // 10단계 이상 꼬리를 물면 멈춤 (잘못된 순환 방지)
        foreach (CardRecipe r in db.recipes)
            if (r.resultCard == card)
                return 1 + Mathf.Max(CardTier(db, r.cardA, depth + 1), CardTier(db, r.cardB, depth + 1));
        return 0;   // 어떤 조합의 결과도 아니면 기본 카드
    }

    // ---------------- 알고 있는지 ----------------

    public static bool IsKnown(CardRecipe recipe)
    {
        return recipe.resultCard != null && EncyclopediaManager.Instance != null
            && EncyclopediaManager.Instance.IsUnlocked(recipe.resultCard.BaseCard.cardName);
    }

    // ---------------- 얻기 ----------------

    public static void Unlock(CardRecipe recipe)
    {
        if (recipe.resultCard == null) return;
        if (EncyclopediaManager.Instance != null)
            EncyclopediaManager.Instance.UnlockCard(recipe.resultCard.cardName);
        RecipeBook.Unlock(recipe.cardA, recipe.cardB);   // 덱 위 조합법 창 기록도 같이
        Debug.Log($"[조합법 획득] {Describe(recipe)}");
    }

    // 아직 모르는 tier단계 조합법 중 하나를 무작위로 얻는다. 모르는 게 없으면 false
    public static bool TryUnlockRandom(CardRecipeDatabase db, int tier, out CardRecipe unlocked)
    {
        unlocked = default;
        if (db == null || db.recipes == null) return false;

        List<CardRecipe> pool = new List<CardRecipe>();
        foreach (CardRecipe r in db.recipes)
            if (r.resultCard != null && GetTier(db, r) == tier && !IsKnown(r))
                pool.Add(r);
        if (pool.Count == 0) return false;

        unlocked = pool[Random.Range(0, pool.Count)];
        Unlock(unlocked);
        return true;
    }

    public static string Describe(CardRecipe r)
    {
        string a = r.cardA != null ? r.cardA.cardName : "?";
        string b = r.cardB != null ? r.cardB.cardName : "?";
        string c = r.resultCard != null ? r.resultCard.cardName : "?";
        return $"{a} + {b} → {c}";
    }
}
