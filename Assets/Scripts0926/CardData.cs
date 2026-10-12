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
    public int value;        // 기본 수치 (힐량 / 공격력 / 디버프량)

    [Header("추가 효과 (선택)")]
    [Tooltip("힐/디버프 카드 사용 시 적에게 줄 추가 데미지 (0이면 회복만 함)")]
    public int damageValue;  // ★ 추가: 힐 카드가 적에게 줄 데미지 수치

    public string statusEffect;
    public int combineId;
    [Tooltip("비워 두면 카드 종류와 Value로 설명이 자동으로 만들어진다")]
    [TextArea(2, 4)] public string description;

    [Header("Visual Asset")]
    public Sprite cardIcon;
}