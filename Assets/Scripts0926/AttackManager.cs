using System.Collections;
using UnityEngine;
using TMPro;

public class AttackManager : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;   // 기존 GameManager 연결
    public Transform[] dropSlots;     // 기존 CardSlot들이 위치한 슬롯 배열 (3개)

    [Header("Enemy Data Asset (선택 사항)")]
    [Tooltip("연결하면 해당 몬스터 수치가 적용되고, 비워두면 아래 기본 수치가 적용됩니다.")]
    public EnemyData currentEnemyData; // 몬스터 데이터 에셋

    [Header("Enemy Stats")]
    public string enemyName = "몬스터";
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

    private void Start()
    {
        // ★ [안전장치] 몬스터 데이터 에셋이 드래그되어 있다면 에셋 값으로 초기화
        if (currentEnemyData != null)
        {
            enemyName = currentEnemyData.enemyName;
            enemyMaxHP = currentEnemyData.maxHP;
            enemyAttackDamage = currentEnemyData.attackDamage;
        }

        enemyCurrentHP = enemyMaxHP;
        playerCurrentHP = playerMaxHP;
        UpdateUI();
    }

    // UI [공격 / 턴 종료] 버튼의 OnClick()에 연결할 함수
    public void OnAttackButtonClicked()
    {
        if (isAttacking) return;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        attackReduction = 0;   // 내 턴 시작: 디버프 초기화

        // 1. 슬롯에 올려진 카드 효과 발동
        foreach (Transform slot in dropSlots)
        {
            if (slot.childCount > 0)
            {
                CardUI cardUI = slot.GetChild(0).GetComponent<CardUI>();

                if (cardUI != null && cardUI.cardData != null)
                {
                    if (cardUI.cardData.cardType == CardType.Attack)
                    {
                        enemyCurrentHP -= cardUI.cardData.value;
                        if (enemyCurrentHP < 0) enemyCurrentHP = 0;
                        UpdateUI();
                        Debug.Log($"[공격] 적에게 {cardUI.cardData.value} 데미지를 주었습니다!");
                    }
                    else if (cardUI.cardData.cardType == CardType.Heal)
                    {
                        int before = playerCurrentHP;
                        playerCurrentHP = Mathf.Min(playerMaxHP, playerCurrentHP + cardUI.cardData.value);
                        int healed = playerCurrentHP - before;
                        UpdateUI();
                        Debug.Log($"[힐] {cardUI.cardData.cardName} → 체력 {healed} 회복! ({before} → {playerCurrentHP})");
                    }
                    else if (cardUI.cardData.cardType == CardType.Debuff)
                    {
                        attackReduction += cardUI.cardData.value;
                        UpdateUI();
                        Debug.Log($"[디버프] 적 공격력 -{cardUI.cardData.value} (이번 턴 공격력: {GetCurrentEnemyAttack()})");
                    }

                    Destroy(cardUI.gameObject);
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }

        yield return new WaitForSeconds(0.5f);

        // 2. 적의 반격
        if (enemyCurrentHP > 0)
        {
            int damage = GetCurrentEnemyAttack();
            playerCurrentHP -= damage;
            if (playerCurrentHP < 0) playerCurrentHP = 0;
            UpdateUI();

            if (attackReduction > 0)
                Debug.Log($"[적 반격] 디버프로 약해진 공격! {enemyAttackDamage} → {damage} 데미지를 받았습니다.");
            else
                Debug.Log($"[적 반격] 플레이어가 {damage} 데미지를 받았습니다!");

            yield return new WaitForSeconds(0.5f);
        }

        // 3. 디버프 해제
        if (attackReduction > 0)
        {
            attackReduction = 0;
            UpdateUI();
            Debug.Log("[디버프] 효과가 끝나 적 공격력이 원래대로 돌아왔습니다.");
        }

        // 4. 턴 보충
        if (gameManager != null)
        {
            gameManager.StartTurn();
        }

        isAttacking = false;
    }

    private int GetCurrentEnemyAttack()
    {
        return Mathf.Max(0, enemyAttackDamage - attackReduction);
    }

    private void UpdateUI()
    {
        if (enemyHPText != null)
            enemyHPText.text = $"{enemyName} HP: {enemyCurrentHP}/{enemyMaxHP}";

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