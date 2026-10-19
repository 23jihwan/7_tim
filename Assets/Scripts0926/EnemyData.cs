using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Combat/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName = "몬스터";
    public ElementType elementType = ElementType.None; // 몬스터 속성 (None, Fire, Water 등)
    public int maxHP = 100;
    public int attackDamage = 10;
    public Sprite enemySprite;
}