using UnityEngine;

public enum CardType
{
    Attack,
    Heal,
    Debuff
}

[CreateAssetMenu(fileName = "New Card", menuName = "Card System/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    public CardType cardType;
    public int value;
    public string statusEffect;
    public int combineId;

    [Header("Visual Asset")]
    public Sprite cardIcon; // ★ 추가: 카드에 들어갈 이미지 (공격/힐/디버프 이미지 등)
}