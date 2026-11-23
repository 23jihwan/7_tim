using UnityEngine;

public enum RelicType
{
    Attack,
    Heal,
    Debuff,
    Utility
}

public enum StatBonusType
{
    None,
    MaxHP,
    GoldBonus,
    AttackBonus,
    DebuffBonus,
    StunBonus,
    CriticalChanceBonus,

    // ★ 신규 5종 유물 효과 타입
    GoldenGrimoire,      // 황금 마도서
    AlchemyScale,        // 연금술의 황금 저울
    OrbOfGreed,          // 탐욕의 보주
    PhilosopherStone,    // 현자의 변환석
    MidasCrown           // 미다스의 왕관
}

[CreateAssetMenu(fileName = "NewRelic", menuName = "Inventory/RelicData")]
public class RelicData : ScriptableObject
{
    public string relicId;
    public string relicName;
    [TextArea] public string description;
    public Sprite icon;
    public RelicType type;

    [Header("효과 설정")]
    public StatBonusType statBonusType;
    public int value;
}