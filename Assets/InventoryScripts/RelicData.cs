using UnityEngine;

// 유물 카테고리
public enum RelicType
{
    Attack,
    Heal,
    Debuff,
    Utility
}

// 유물 능력치 부여 타입 (치명타 확률 추가)
public enum StatBonusType
{
    None,
    MaxHP,
    GoldBonus,
    AttackBonus,
    DebuffBonus,
    StunBonus,
    CriticalChanceBonus // ★ 치명타 확률 증가 효과
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
    public StatBonusType statBonusType; // 어떤 능력치를 올려주는 유물인가?
    public int value;                   // 올려주는 수치 (+값, 치명타의 경우 % 확률 수치)
}