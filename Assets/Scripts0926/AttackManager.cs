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

    // 적의 이번 턴 행동 불가(스턴/빙결/마비) 플래그
    private bool isEnemyStunned = false;

    private List<ActiveStatusEffect> activeEnemyEffects = new List<ActiveStatusEffect>();

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

        // ★ GameManager 초기 체력 설정 및 연동
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
        isEnemyStunned = false; // 턴 시작 시 스턴 상태 초기화

        // 1. 카드 실행 연산
        foreach (Transform slot in dropSlots)
        {
            if (slot.childCount > 0)
            {
                CardUI cardUI = slot.GetChild(0).GetComponent<CardUI>();

                if (cardUI != null && cardUI.cardData != null)
                {
                    CardData card = cardUI.cardData;

                    // (0) 카드 효과로 행동 불가(Causes Stun) 옵션이 있다면 활성화
                    if (card.causesStun)
                    {
                        isEnemyStunned = true;
                        Debug.Log($"[상태이상] {card.cardName} 카드의 효과로 {enemyName}이(가) 이번 턴 행동 불가 상태가 되었습니다!");
                    }

                    // (1) 공격 카드
                    if (card.cardType == CardType.Attack)
                    {
                        int baseDamage = card.value;

                        if (enemyElement != ElementType.None && card.bonusDamageAgainstElemental > 0)
                        {
                            baseDamage += card.bonusDamageAgainstElemental;
                            Debug.Log($"[속성 보너스] 적({enemyElement})이 속성을 보유하여 추가 데미지 +{card.bonusDamageAgainstElemental} 적용!");
                        }

                        float multiplier = GetElementMultiplier(card.elementType, enemyElement);
                        int finalDamage = Mathf.RoundToInt(baseDamage * multiplier);

                        // ★ GameManager를 통해 적 HP 감축 (구체/텍스트 연동)
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.TakeEnemyDamage(finalDamage);
                            enemyCurrentHP = (int)GameManager.Instance.enemyCurrentHP;
                        }
                        else
                        {
                            enemyCurrentHP = Mathf.Max(0, enemyCurrentHP - finalDamage);
                        }

                        UpdateUI();
                        Debug.Log($"[공격] {card.cardName}({card.elementType}) -> {enemyName}({enemyElement})에게 {finalDamage} 데미지!");
                    }
                    // (2) 힐 카드
                    else if (card.cardType == CardType.Heal)
                    {
                        int before = playerCurrentHP;

                        // ★ GameManager를 통해 플레이어 HP 회복 (구체 체력바 채워짐)
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.HealPlayer(card.value);
                            playerCurrentHP = (int)GameManager.Instance.playerCurrentHP;
                        }
                        else
                        {
                            playerCurrentHP = Mathf.Min(playerMaxHP, playerCurrentHP + card.value);
                        }

                        int healed = playerCurrentHP - before;
                        Debug.Log($"[힐] {card.cardName} → 체력 {healed} 회복!");

                        if (card.damageValue > 0)
                        {
                            int baseDamage = card.damageValue;

                            if (enemyElement != ElementType.None && card.bonusDamageAgainstElemental > 0)
                            {
                                baseDamage += card.bonusDamageAgainstElemental;
                                Debug.Log($"[속성 보너스] 힐 추가 공격 데미지 +{card.bonusDamageAgainstElemental} 적용!");
                            }

                            float multiplier = GetElementMultiplier(card.elementType, enemyElement);
                            int finalDamage = Mathf.RoundToInt(baseDamage * multiplier);

                            // ★ 신성 힐 공격 추가 데미지 연동
                            if (GameManager.Instance != null)
                            {
                                GameManager.Instance.TakeEnemyDamage(finalDamage);
                                enemyCurrentHP = (int)GameManager.Instance.enemyCurrentHP;
                            }
                            else
                            {
                                enemyCurrentHP = Mathf.Max(0, enemyCurrentHP - finalDamage);
                            }

                            Debug.Log($"[신성 힐 공격] 적에게 {finalDamage} 데미지!");
                        }
                        UpdateUI();
                    }
                    // (3) 디버프 카드
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

                // ★ GameManager 지속 피해 연동
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TakeEnemyDamage(finalDotDamage);
                    enemyCurrentHP = (int)GameManager.Instance.enemyCurrentHP;
                }
                else
                {
                    enemyCurrentHP = Mathf.Max(0, enemyCurrentHP - finalDotDamage);
                }

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
            // 행동 불가(스턴/빙결/마비) 상태면 이번 턴 공격 무효화
            if (isEnemyStunned)
            {
                Debug.Log($"[적 행동 불가] {enemyName}이(가) 스턴/빙결/마비 효과로 기절하여 이번 턴에 공격하지 못합니다!");
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                int damage = GetCurrentEnemyAttack();

                // ★ 적이 플레이어를 공격할 때 GameManager를 호출하여 피 구체 감소!
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TakePlayerDamage(damage);
                    playerCurrentHP = (int)GameManager.Instance.playerCurrentHP;
                }
                else
                {
                    playerCurrentHP = Mathf.Max(0, playerCurrentHP - damage);
                }

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

            if (isEnemyStunned)
            {
                statusTag += " [행동불가]";
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