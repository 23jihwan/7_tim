using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class ActiveStatusEffect
{
    public string effectName;
    public ElementType element;
    public int damagePerTurn;
    public int remainingTurns;

    public ActiveStatusEffect(string effectName, ElementType element, int damagePerTurn, int remainingTurns)
    {
        this.effectName = effectName;
        this.element = element;
        this.damagePerTurn = damagePerTurn;
        this.remainingTurns = remainingTurns;
    }
}

public class AttackManager : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    public Transform[] dropSlots;

    [Header("Animation")]
    [Tooltip("카드가 날아가고 흔들릴 몬스터 그림 (Monster_Skeleton 등). 비워 두면 흔들림 없이 진행")]
    public RectTransform monsterVisual;

    [Header("Enemy Data Asset (선택 사항)")]
    public EnemyData currentEnemyData;

    [Header("Enemy Stats")]
    public string enemyName = "몬스터";
    public ElementType enemyElement = ElementType.None;
    public int enemyMaxHP = 100;
    public int enemyCurrentHP;
    public int enemyAttackDamage = 10;
    public TextMeshProUGUI enemyHPText;

    [Header("Player Stats")]
    public int playerMaxHP = 100;
    public int playerCurrentHP;
    public TextMeshProUGUI playerHPText;

    [Header("Debuff (적 공격력 감소)")]
    public TextMeshProUGUI enemyAttackText;

    private int attackReduction = 0;
    private bool isAttacking = false;

    private int enemyStunTurns = 0;

    private List<ActiveStatusEffect> activeEnemyEffects = new List<ActiveStatusEffect>();

    // 애니메이션 색
    private static readonly Color AttackColor = new Color32(0xE8, 0x5A, 0x3C, 0xFF);
    private static readonly Color HealColor = new Color32(0x6F, 0xD8, 0x7A, 0xFF);
    private static readonly Color DebuffColor = new Color32(0x6F, 0xB8, 0xF0, 0xFF);
    private static readonly Color HolyColor = new Color32(0xFF, 0xF0, 0x9A, 0xFF);

    private void Start()
    {
        if (currentEnemyData != null)
        {
            enemyName = currentEnemyData.enemyName;
            enemyElement = currentEnemyData.elementType;
            enemyMaxHP = currentEnemyData.maxHP;
            enemyAttackDamage = currentEnemyData.attackDamage;
        }

        enemyCurrentHP = enemyMaxHP;
        playerCurrentHP = playerMaxHP;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.playerMaxHP = playerMaxHP;
            GameManager.Instance.playerCurrentHP = playerCurrentHP;
            GameManager.Instance.enemyMaxHP = enemyMaxHP;
            GameManager.Instance.enemyCurrentHP = enemyCurrentHP;
            GameManager.Instance.UpdatePlayerHPUI();
            GameManager.Instance.UpdateEnemyHPUI();
        }

        UpdateUI();
    }

    public void OnAttackButtonClicked()
    {
        if (isAttacking) return;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        attackReduction = 0;

        // 1. 카드 실행 연산
        foreach (Transform slot in dropSlots)
        {
            if (slot.childCount > 0)
            {
                CardUI cardUI = slot.GetChild(0).GetComponent<CardUI>();

                if (cardUI != null && cardUI.cardData != null)
                {
                    CardData card = cardUI.cardData;

                    Color glow = card.cardType == CardType.Attack ? AttackColor
                               : card.cardType == CardType.Heal ? HealColor
                               : DebuffColor;
                    RectTransform target = (card.cardType == CardType.Heal && playerHPText != null) ? playerHPText.rectTransform : monsterVisual;
                    yield return StartCoroutine(CardAnimator.UseCard(cardUI, target, glow));

                    // ★ [유물 연동] 카드 효과로 행동 불가가 발생할 때 스턴 유물 보너스(+1턴 등) 추가 적용
                    if (card.causesStun)
                    {
                        int addTurns = card.stunDuration > 0 ? card.stunDuration : 1;

                        // 유물로 인한 추가 스턴 턴 수 합산 (+)
                        int relicStunBonus = (RunState.Instance != null) ? RunState.Instance.GetTotalRelicBonus(StatBonusType.StunBonus) : 0;
                        addTurns += relicStunBonus;

                        enemyStunTurns += addTurns;
                        Debug.Log($"[상태이상] {card.cardName} 카드의 효과로 {enemyName}이(가) {addTurns}턴 동안 행동 불가 상태가 됩니다! (유물 보너스 +{relicStunBonus} 포함, 총 남은 턴: {enemyStunTurns}턴)");
                    }

                    // (1) 공격 카드
                    if (card.cardType == CardType.Attack)
                    {
                        int baseDamage = card.value;

                        int relicAttackBonus = (RunState.Instance != null) ? RunState.Instance.GetTotalRelicBonus(StatBonusType.AttackBonus) : 0;
                        baseDamage += relicAttackBonus;

                        if (enemyElement != ElementType.None && card.bonusDamageAgainstElemental > 0)
                        {
                            baseDamage += card.bonusDamageAgainstElemental;
                            Debug.Log($"[속성 보너스] 적({enemyElement})이 속성을 보유하여 추가 데미지 +{card.bonusDamageAgainstElemental} 적용!");
                        }

                        float multiplier = GetElementMultiplier(card.elementType, enemyElement);
                        int finalDamage = Mathf.RoundToInt(baseDamage * multiplier);

                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.TakeEnemyDamage(finalDamage);
                            enemyCurrentHP = (int)GameManager.Instance.enemyCurrentHP;
                        }
                        else
                        {
                            enemyCurrentHP = Mathf.Max(0, enemyCurrentHP - finalDamage);
                        }

                        CardAnimator.Hit(monsterVisual, AttackColor);
                        UpdateUI();
                        Debug.Log($"[공격] {card.cardName}({card.elementType}) -> {enemyName}({enemyElement})에게 {finalDamage} 데미지!");
                    }
                    // (2) 힐 카드
                    else if (card.cardType == CardType.Heal)
                    {
                        int before = playerCurrentHP;

                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.HealPlayer(card.value);
                            playerCurrentHP = (int)GameManager.Instance.playerCurrentHP;
                        }
                        else
                        {
                            playerCurrentHP = Mathf.Min(playerMaxHP, playerCurrentHP + card.value);
                        }

                        if (playerHPText != null) CardAnimator.Pulse(playerHPText.rectTransform, HealColor);

                        int healed = playerCurrentHP - before;
                        Debug.Log($"[힐] {card.cardName} → 체력 {healed} 회복!");

                        if (card.damageValue > 0)
                        {
                            int baseDamage = card.damageValue;

                            int relicAttackBonus = (RunState.Instance != null) ? RunState.Instance.GetTotalRelicBonus(StatBonusType.AttackBonus) : 0;
                            baseDamage += relicAttackBonus;

                            if (enemyElement != ElementType.None && card.bonusDamageAgainstElemental > 0)
                            {
                                baseDamage += card.bonusDamageAgainstElemental;
                            }

                            float multiplier = GetElementMultiplier(card.elementType, enemyElement);
                            int finalDamage = Mathf.RoundToInt(baseDamage * multiplier);

                            if (GameManager.Instance != null)
                            {
                                GameManager.Instance.TakeEnemyDamage(finalDamage);
                                enemyCurrentHP = (int)GameManager.Instance.enemyCurrentHP;
                            }
                            else
                            {
                                enemyCurrentHP = Mathf.Max(0, enemyCurrentHP - finalDamage);
                            }

                            CardAnimator.Hit(monsterVisual, HolyColor);
                            Debug.Log($"[신성 힐 공격] 적에게 {finalDamage} 데미지!");
                        }
                        UpdateUI();
                    }
                    // (3) 디버프 카드
                    else if (card.cardType == CardType.Debuff)
                    {
                        int debuffValue = card.value;

                        // ★ [유물 연동] 디버프 효과를 증폭시키는 유물 보너스(+값) 추가 합산
                        int relicDebuffBonus = (RunState.Instance != null) ? RunState.Instance.GetTotalRelicBonus(StatBonusType.DebuffBonus) : 0;
                        debuffValue += relicDebuffBonus;

                        attackReduction += debuffValue;
                        CardAnimator.Hit(monsterVisual, DebuffColor);
                        UpdateUI();
                        Debug.Log($"[디버프] 적 공격력 -{debuffValue} (기본 {card.value} + 유물 보너스 +{relicDebuffBonus})");
                    }

                    // (4) 지속 속성 피해(DOT) 부여
                    if (card.dotDuration > 0 && card.dotDamagePerTurn > 0)
                    {
                        activeEnemyEffects.Add(new ActiveStatusEffect(
                            card.cardName,
                            card.dotElementType,
                            card.dotDamagePerTurn,
                            card.dotDuration
                        ));

                        Debug.Log($"[상태이상 부여] {enemyName}에게 [{card.dotElementType}] 지속 피해 부여! (턴당 {card.dotDamagePerTurn} / {card.dotDuration}턴)");
                    }

                    Destroy(cardUI.gameObject);
                    yield return new WaitForSeconds(0.3f);
                }
            }
        }

        yield return new WaitForSeconds(0.3f);

        // 2. 턴 종료 시 지속 속성 피해(DOT) 발동
        if (activeEnemyEffects.Count > 0 && enemyCurrentHP > 0)
        {
            Debug.Log("--- [지속 속성 피해 발동] ---");

            for (int i = activeEnemyEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeEnemyEffects[i];

                float multiplier = GetElementMultiplier(effect.element, enemyElement);
                int finalDotDamage = Mathf.RoundToInt(effect.damagePerTurn * multiplier);

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TakeEnemyDamage(finalDotDamage);
                    enemyCurrentHP = (int)GameManager.Instance.enemyCurrentHP;
                }
                else
                {
                    enemyCurrentHP = Mathf.Max(0, enemyCurrentHP - finalDotDamage);
                }

                CardAnimator.Hit(monsterVisual, AttackColor);
                effect.remainingTurns--;
                UpdateUI();

                Debug.Log($"[지속 피해] {effect.effectName}의 [{effect.element}] 피해! -> {enemyName}에게 {finalDotDamage} 데미지! (남은 턴: {effect.remainingTurns})");

                yield return new WaitForSeconds(0.4f);

                if (effect.remainingTurns <= 0)
                {
                    Debug.Log($"[상태이상 해제] {effect.effectName}의 [{effect.element}] 지속 효과가 종료되었습니다.");
                    activeEnemyEffects.RemoveAt(i);
                }
            }
        }

        yield return new WaitForSeconds(0.5f);

        // 3. 적의 반격 (살아있는 경우)
        if (enemyCurrentHP > 0)
        {
            if (enemyStunTurns > 0)
            {
                enemyStunTurns--;
                Debug.Log($"[적 행동 불가] {enemyName}이(가) 스턴 효과로 이번 턴에 공격하지 못합니다! (남은 행동불가: {enemyStunTurns}턴)");
                UpdateUI();
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                int damage = GetCurrentEnemyAttack();

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TakePlayerDamage(damage);
                    playerCurrentHP = (int)GameManager.Instance.playerCurrentHP;
                }
                else
                {
                    playerCurrentHP = Mathf.Max(0, playerCurrentHP - damage);
                }

                if (playerHPText != null) CardAnimator.Hit(playerHPText.rectTransform, AttackColor);
                UpdateUI();
                yield return new WaitForSeconds(0.5f);
            }
        }

        if (attackReduction > 0)
        {
            attackReduction = 0;
            UpdateUI();
        }

        if (gameManager != null)
        {
            gameManager.StartTurn();
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.StartTurn();
        }

        isAttacking = false;
    }

    private float GetElementMultiplier(ElementType attacker, ElementType defender)
    {
        if (attacker == ElementType.None || defender == ElementType.None) return 1.0f;

        if (attacker == ElementType.Fire && defender == ElementType.Grass) return 1.5f;
        if (attacker == ElementType.Fire && defender == ElementType.Water) return 0.5f;

        if (attacker == ElementType.Water && defender == ElementType.Fire) return 1.5f;
        if (attacker == ElementType.Water && defender == ElementType.Grass) return 0.5f;

        if (attacker == ElementType.Grass && defender == ElementType.Water) return 1.5f;
        if (attacker == ElementType.Grass && defender == ElementType.Fire) return 0.5f;

        if ((attacker == ElementType.Light && defender == ElementType.Dark) ||
            (attacker == ElementType.Dark && defender == ElementType.Light))
        {
            return 1.5f;
        }

        if (attacker == ElementType.Ice && defender == ElementType.Grass) return 1.5f;
        if (attacker == ElementType.Ice && defender == ElementType.Fire) return 0.5f;

        if (attacker == ElementType.Electric && defender == ElementType.Water) return 1.5f;
        if (attacker == ElementType.Electric && defender == ElementType.Grass) return 0.5f;

        return 1.0f;
    }

    private int GetCurrentEnemyAttack()
    {
        return Mathf.Max(0, enemyAttackDamage - attackReduction);
    }

    private void UpdateUI()
    {
        if (enemyHPText != null)
        {
            string elementTag = enemyElement != ElementType.None ? $"[{enemyElement}] " : "";
            string statusTag = activeEnemyEffects.Count > 0 ? $" (상태이상 {activeEnemyEffects.Count}개)" : "";

            if (enemyStunTurns > 0)
            {
                statusTag += $" [행동불가 {enemyStunTurns}턴]";
            }

            enemyHPText.text = $"{elementTag}{enemyName} HP: {enemyCurrentHP}/{enemyMaxHP}{statusTag}";
        }

        if (playerHPText != null)
            playerHPText.text = $"Player HP: {playerCurrentHP}/{playerMaxHP}";

        if (enemyAttackText != null)
        {
            if (attackReduction > 0)
            {
                enemyAttackText.text = $"공격력: {GetCurrentEnemyAttack()} (-{attackReduction})";
                enemyAttackText.color = new Color32(0x7F, 0xD8, 0xEA, 0xFF);
            }
            else
            {
                enemyAttackText.text = $"공격력: {enemyAttackDamage}";
                enemyAttackText.color = Color.white;
            }
        }
    }
}