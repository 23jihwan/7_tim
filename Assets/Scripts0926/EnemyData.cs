using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Combat/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName = "몬스터";
    public int maxHP = 100;
    public int attackDamage = 10;
    public Sprite enemySprite; // 필요 시 몬스터 이미지

}

