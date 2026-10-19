using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// 몬스터에게 걸린 지속 상태이상 데이터 구조
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

    // 몬스터에게 부여된 지속 속성 데미지(DOT) 리스트
    private List<ActiveStatusEffect> activeEnemyEffects = new List<ActiveStatusEffect>();

    private void Start()
    {
        // 안전장치: EnemyData 에셋이 연결되어 있으면 해당 데이터로 초기화
        if (currentEnemyData != null)
        {
            enemyName = currentEnemyData.enemyName;
            enemyElement = currentEnemyData.elementType;
            enemyMaxHP = currentEnemyData.maxHP;
            enemyAttackDamage = currentEnemyData.attackDamage;
        }

        enemyCurrentHP = enemyMaxHP;
        playerCurrentHP = playerMaxHP;
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

        // 1. 카드 발동 및 지속 상태이상 부여
        foreach (Transform slot in dropSlots)
        {
            if (slot.childCount > 0)
            {
                CardUI cardUI = slot.GetChild(0).GetComponent<CardUI>();

                if (cardUI != null && cardUI.cardData != null)
                {
                    CardData card = cardUI.cardData;

                    // (1) 공격 카드 로직
                    if (card.cardType == CardType.Attack)
                    {
                        int baseDamage = card.value;

                        // 속성 몬스터 대상 추가 데미지 보너스
                        if (enemyElement != ElementType.None && card.bonusDamageAgainstElemental > 0)
                        {
                            baseDamage += card.bonusDamageAgainstElemental;
                            Debug.Log($"[속성 보너스] 적({enemyElement})이 속성을 보유하여 데미지 +{card.bonusDamageAgainstElemental} 적용!");
                        }

                        float multiplier = GetElementMultiplier(card.elementType, enemyElement);
                        int finalDamage = Mathf.RoundToInt(baseDamage * multiplier);

                        enemyCurrentHP -= finalDamage;
                        if (enemyCurrentHP < 0) enemyCurrentHP = 0;
                        UpdateUI();

                        Debug.Log($"[공격] {card.cardName}({card.elementType}) -> {enemyName}({enemyElement})에게 {finalDamage} 데미지!");
                    }
                    // (2) 힐 카드 로직
                    else if (card.cardType == CardType.Heal)
                    {
                        int before = playerCurrentHP;
                        playerCurrentHP = Mathf.Min(playerMaxHP, playerCurrentHP + card.value);
                        int healed = playerCurrentHP - before;
                        Debug.Log($"[힐] {card.cardName} → 체력 {healed} 회복!");

                        if (card.damageValue > 0)
                        {
                            int baseDamage = card.damageValue;

                            if (enemyElement != ElementType.None && card.bonusDamageAgainstElemental > 0)
                            {
                                baseDamage += card.bonusDamageAgainstElemental;
                                Debug.Log($"[속성 보너스] 힐 추가 공격에 데미지 +{card.bonusDamageAgainstElemental} 적용!");
                            }

                            float multiplier = GetElementMultiplier(card.elementType, enemyElement);
                            int finalDamage = Mathf.RoundToInt(baseDamage * multiplier);

                            enemyCurrentHP -= finalDamage;
                            if (enemyCurrentHP < 0) enemyCurrentHP = 0;
                            Debug.Log($"[신성 힐 공격] 적에게 {finalDamage} 데미지!");
                        }
                        UpdateUI();
                    }
                    // (3) 디버프 카드 로직
                    else if (card.cardType == CardType.Debuff)
                    {
                        attackReduction += card.value;
                        UpdateUI();
                        Debug.Log($"[디버프] 적 공격력 -{card.value}");
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
                    yield return new WaitForSeconds(0.5f);
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

                enemyCurrentHP -= finalDotDamage;
                if (enemyCurrentHP < 0) enemyCurrentHP = 0;

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

        // 3. 적의 반격
        if (enemyCurrentHP > 0)
        {
            int damage = GetCurrentEnemyAttack();
            playerCurrentHP -= damage;
            if (playerCurrentHP < 0) playerCurrentHP = 0;
            UpdateUI();
            yield return new WaitForSeconds(0.5f);
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

        isAttacking = false;
    }

    // 속성 상성 배율 계산 (불>풀>물>불, 빛<->어둠)
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