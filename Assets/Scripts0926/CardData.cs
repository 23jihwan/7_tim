using UnityEngine;

// 1. 속성 종류 정의
public enum ElementType
{
    None,   // 무속성
    Fire,   // 불
    Water,  // 물
    Grass,  // 풀
    Light,  // 빛
    Dark    // 어둠
}

// 2. 카드 타입 정의
public enum CardType
{
    Attack,
    Heal,
    Debuff
}

[CreateAssetMenu(fileName = "New Card", menuName = "Card System/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;                             // 카드 이름

    [TextArea(2, 5)]
    public string description;                          // ★ 카드 설명문 (CardUI 에러 해결)

    public CardType cardType;                           // 카드 타입 (공격, 힐, 디버프)
    public ElementType elementType = ElementType.None;  // 카드의 기본 속성

    public int value;                                   // 기본 수치 (공격력 / 힐량 / 디버프량)

    [Header("추가 데미지 효과 (선택)")]
    [Tooltip("힐/디버프 카드 사용 시 적에게 줄 추가 데미지 (0이면 회복만 함)")]
    public int damageValue;

    [Tooltip("적 몬스터가 속성(None이 아닌 속성)을 가지고 있을 때 추가할 보너스 데미지")]
    public int bonusDamageAgainstElemental = 0;

    [Header("지속 속성 데미지 (DOT) 설정")]
    [Tooltip("지속 데미지의 속성 (Fire, Water, Grass 등)")]
    public ElementType dotElementType = ElementType.None;

    [Tooltip("매 턴마다 줄 속성 데미지 수치 (0이면 지속 데미지 없음)")]
    public int dotDamagePerTurn = 0;

    [Tooltip("지속 데미지가 유지될 턴 수 (0이면 지속 데미지 없음)")]
    public int dotDuration = 0;

    public string statusEffect;
    public int combineId;

    [Header("Visual Asset")]
    public Sprite cardIcon;                             // 카드 이미지
}