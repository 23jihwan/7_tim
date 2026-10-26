using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Combat/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("몬스터 기본 정보")]
    public string enemyName = "몬스터";
    public ElementType elementType = ElementType.None; // 몬스터 속성

    [Header("몬스터 스탯")]
    public int maxHP = 100;
    public int attackDamage = 10;

    [Tooltip("고정 차감 방어력 (예: 2 입력 시 최종 피해 2 감소)")]
    public int defense = 0; // ★ 추가된 방어력 필드 (기본값 0)

    [Header("몬스터 이미지")]
    public Sprite enemySprite;
}