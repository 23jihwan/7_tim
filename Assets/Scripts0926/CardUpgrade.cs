using System.Text.RegularExpressions;
using UnityEngine;

// 카드 강화: 원래 카드를 복사해서 수치만 올린 "강화 카드"를 만든다 (최대 +5)
// 강화 카드는 baseCard에 원래 카드를 기억하므로 조합, 도감에서는 원래 카드로 취급된다
public static class CardUpgrade
{
    public const int MaxLevel = 5;
    public const float StepPercent = 0.2f;   // 한 단계마다 원래 수치의 20%씩 (최소 +1)

    public static bool CanUpgrade(CardData card)
    {
        return card != null && card.upgradeLevel < MaxLevel;
    }

    public static CardData Upgrade(CardData card)
    {
        if (!CanUpgrade(card)) return card;

        CardData b = card.BaseCard;
        int lv = card.upgradeLevel + 1;

        CardData u = Object.Instantiate(b);   // 원래 카드를 그대로 복사
        u.name = $"{b.name}+{lv}";
        u.baseCard = b;
        u.upgradeLevel = lv;
        u.cardName = $"{b.cardName}+{lv}";

        // 효과 수치 올리기
        u.value = Up(b.value, lv);
        u.damageValue = Up(b.damageValue, lv);
        u.shieldValue = Up(b.shieldValue, lv);
        u.buffValue = Up(b.buffValue, lv);
        u.dotDamagePerTurn = Up(b.dotDamagePerTurn, lv);

        // 도감 상세 팝업용 수치도 같이
        u.attack = Up(b.attack, lv);
        u.health = Up(b.health, lv);
        u.debuff = Up(b.debuff, lv);
        u.shield = Up(b.shield, lv);
        u.buff = Up(b.buff, lv);

        // 직접 쓴 설명문 안의 숫자도 바뀐 수치로 바꿔 준다 (예: "10 데미지" → "12 데미지")
        if (!string.IsNullOrEmpty(b.description))
        {
            string d = b.description;
            d = ReplaceNumber(d, b.value, u.value);
            d = ReplaceNumber(d, b.damageValue, u.damageValue);
            d = ReplaceNumber(d, b.shieldValue, u.shieldValue);
            d = ReplaceNumber(d, b.buffValue, u.buffValue);
            d = ReplaceNumber(d, b.dotDamagePerTurn, u.dotDamagePerTurn);
            u.description = d;
        }
        return u;
    }

    // 강화 미리보기용: 다음 단계의 주 수치
    public static int NextValue(CardData card)
    {
        CardData b = card.BaseCard;
        return Up(b.value, card.upgradeLevel + 1);
    }

    private static int Up(int baseValue, int level)
    {
        if (baseValue <= 0) return baseValue;
        int step = Mathf.Max(1, Mathf.RoundToInt(baseValue * StepPercent));
        return baseValue + step * level;
    }

    private static string ReplaceNumber(string text, int from, int to)
    {
        if (from <= 1 || from == to) return text;   // 0, 1은 턴 수 등과 헷갈릴 수 있어 건드리지 않음
        return Regex.Replace(text, $@"(?<!\d){from}(?!\d)", to.ToString());
    }
}
