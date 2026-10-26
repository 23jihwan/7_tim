using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerData", menuName = "Player Data")]
public class PlayerData : ScriptableObject
{
    // [Header("플레이어 기본 정보")]
    // public string playerName = "용사"; // 추후 필요 시 주석 해제하여 사용

    [Header("기본 스탯")]
    public int maxHP = 100;

    [Tooltip("플레이어 고정 방어력 (예: 2 입력 시 데미지 2 감소)")]
    public int defense = 2;

    // public int baseAttackDamage = 10; // 카드 수치로만 데미지를 주므로 주석 처리

    // [Header("이미지")]
    // public Sprite playerPortrait; // 추후 필요 시 주석 해제하여 사용
}