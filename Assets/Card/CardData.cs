using UnityEngine;

// 1. 속성 종류 정의
public enum ElementType
{
    None,      // 무속성
    Fire,      // 불
    Water,     // 물
    Grass,     // 풀
    Light,     // 빛
    Dark,      // 어둠
    Ice,       // 빙결
    Electric   // 전기
}

// 2. 카드 타입 정의
public enum CardType
{
    Attack,
    Heal,
    Debuff,
    Shield,    // 배리어 / 실드 카드 타입
    Buff       // ★ [추가] 버프 카드 타입
}

[CreateAssetMenu(fileName = "New Card", menuName = "Card System/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;                             // 카드 이름

    [TextArea(2, 5)]
    public string description;                          // 카드 설명문

    public CardType cardType;                           // 카드 타입 (공격, 힐, 디버프, 실드, 버프)
    public ElementType elementType = ElementType.None;  // 카드의 기본 속성

    public int value;                                   // 기본 수치 (공격력 / 힐량 / 디버프량)

    [Header("실드 / 배리어 설정")]
    [Tooltip("이번 턴 동안 적의 공격 피해를 적게 받게 해주는 배리어 수치 (예: 5, 10, 15)")]
    public int shieldValue = 0;                         // 배리어 수치

    [Header("버프 / 강화 설정")]
    [Tooltip("이번 턴 동안 플레이어의 공격력을 올려주는 버프 수치 (예: 5, 10)")]
    public int buffValue = 0;                           // ★ [추가] 공격력 버프 수치
    [Tooltip("버프 지속 턴 수 (1 = 이번 턴)")]
    public int buffDuration = 1;                        // ★ [추가] 버프 지속 턴 수

    [Header("추가 데미지 및 특수 효과 (인스펙터 설정)")]
    [Tooltip("힐/디버프 카드 사용 시 적에게 줄 추가 데미지 (0이면 회복만 함)")]
    public int damageValue;

    [Tooltip("적 몬스터가 속성(None이 아닌 속성)을 가지고 있을 때 추가할 보너스 데미지")]
    public int bonusDamageAgainstElemental = 0;

    [Tooltip("체크 시 적을 이번 턴에 행동 불가(기절/빙결/마비) 상태로 만듭니다.")]
    public bool causesStun = false;                     // 행동 불가 여부 스위치

    [Tooltip("행동 불가(기절/빙결/마비)가 유지될 턴 수 (1 = 1턴, 2 = 2턴 등)")]
    public int stunDuration = 1;                        // 지속 턴 수 설정 변수

    [Header("지속 속성 데미지 (DOT) 설정")]
    [Tooltip("지속 데미지의 속성 (Fire, Water, Grass, Ice, Electric 등)")]
    public ElementType dotElementType = ElementType.None;

    [Tooltip("매 턴마다 줄 속성 데미지 수치 (0이면 지속 데미지 없음)")]
    public int dotDamagePerTurn = 0;

    [Tooltip("지속 데미지가 유지될 턴 수 (0이면 지속 데미지 없음)")]
    public int dotDuration = 0;

    public string statusEffect;
    public int combineId;

    [Header("도감 및 UI 표시용 스탯")]
    public int attack;                                  // 도감 상세 팝업용 공격력
    public int health;                                  // 도감 상세 팝업용 체력 / 회복량
    public int debuff;                                  // 도감 상세 팝업용 디버프 수치
    public int shield;                                  // 도감 상세 팝업용 쉴드/배리어 수치
    public int buff;                                    // ★ [추가] 도감 상세 팝업용 버프 수치

    [Header("Visual Asset")]
    public Sprite cardIcon;                             // 카드 이미지
}