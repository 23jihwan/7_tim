using UnityEngine;

public enum RelicType { Attack, Heal, Debuff, Utility }

// ★ 디버프 추가 감소와 스턴 턴 수 추가 효과 타입 포함
public enum StatBonusType { None, MaxHP, GoldBonus, AttackBonus, DebuffBonus, StunBonus }

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
    public int value;                   // 올려주는 수치 (+값)
}