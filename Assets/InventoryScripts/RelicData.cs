using UnityEngine;

public enum RelicType { Attack, Heal, Debuff, Utility }

[CreateAssetMenu(fileName = "NewRelic", menuName = "Inventory/RelicData")]
public class RelicData : ScriptableObject
{
    public string relicId;
    public string relicName;
    [TextArea] public string description;
    public Sprite icon;
    public RelicType type;
    public int value;
}