using UnityEngine;

public enum RelicType
{
    Attack,
    Heal,
    Debuff,
    Utility
}

// StatBonusType에 CriticalChanceBonus 추가
public enum StatBonusType
{
    None,
    MaxHP,
    GoldBonus,
    AttackBonus,
    DebuffBonus,
    StunBonus,
    CriticalChanceBonus // ★ 치명타 확률 증가 보너스
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
    public StatBonusType statBonusType; // CriticalChanceBonus 선택
    public int value;                   // 치명타 확률 수치 (예: 20 -> 20%)
}