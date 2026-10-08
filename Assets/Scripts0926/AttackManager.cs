using System.Collections;
using UnityEngine;
using TMPro;

public class AttackManager : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;   // 기존 GameManager 연결
    public Transform[] dropSlots;     // 기존 CardSlot들이 위치한 슬롯 배열 (3개)

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
    [Tooltip("적 공격력을 표시할 글자 (비워 둬도 동작함)")]
    public TextMeshProUGUI enemyAttackText;

    // 이번 턴에 디버프 카드로 깎인 적 공격력. 적의 공격이 끝나면 0으로 돌아간다
    private int attackReduction = 0;

    private bool isAttacking = false;

    private void Start()
    {
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
                        // 최대 체력을 넘지 않게 회복
                        int before = playerCurrentHP;
                        playerCurrentHP = Mathf.Min(playerMaxHP, playerCurrentHP + cardUI.cardData.value);
                        int healed = playerCurrentHP - before;
                        UpdateUI();
                        Debug.Log($"[힐] {cardUI.cardData.cardName} → 체력 {healed} 회복! ({before} → {playerCurrentHP})");
                    }
                    else if (cardUI.cardData.cardType == CardType.Debuff)
                    {
                        // 이번 턴에만 적 공격력 감소 (카드의 value만큼, 기본 3)
                        attackReduction += cardUI.cardData.value;
                        UpdateUI();
                        Debug.Log($"[디버프] 적 공격력 -{cardUI.cardData.value} (이번 턴 공격력: {GetCurrentEnemyAttack()})");
                    }

                    // 카드를 사용했으므로 GameObject 파괴
                    Destroy(cardUI.gameObject);
                    yield return new WaitForSeconds(0.5f); // 연출 간격 대기
                }
            }
        }

        yield return new WaitForSeconds(0.5f);

        // 2. 적의 반격 (적이 살아있는 경우) - 디버프로 깎인 만큼 약해진다
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

        // 3. 적의 공격이 끝났으므로 디버프 효과 해제
        if (attackReduction > 0)
        {
            attackReduction = 0;
            UpdateUI();
            Debug.Log("[디버프] 효과가 끝나 적 공격력이 원래대로 돌아왔습니다.");
        }

        // 4. 기존 GameManager의 StartTurn()을 호출하여 부족한 손패(7장) 재보충
        if (gameManager != null)
        {
            gameManager.StartTurn();
        }

        isAttacking = false;
    }

    // 디버프를 반영한 이번 턴 적 공격력 (0보다 작아지지 않음)
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
                enemyAttackText.color = new Color32(0x7F, 0xD8, 0xEA, 0xFF);   // 약해짐: 하늘색
            }
            else
            {
                enemyAttackText.text = $"공격력: {enemyAttackDamage}";
                enemyAttackText.color = Color.white;                           // 평소: 흰색
            }
        }
    }
}