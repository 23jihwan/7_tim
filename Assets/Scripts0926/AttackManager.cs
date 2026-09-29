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

        // 1. 슬롯에 올려진 카드의 공격력만큼 적 공격
        foreach (Transform slot in dropSlots)
        {
            if (slot.childCount > 0)
            {
                CardUI cardUI = slot.GetChild(0).GetComponent<CardUI>();

                if (cardUI != null && cardUI.cardData != null)
                {
                    // CardType이 Attack일 때 value만큼 데미지 차감
                    if (cardUI.cardData.cardType == CardType.Attack)
                    {
                        enemyCurrentHP -= cardUI.cardData.value;

                        if (enemyCurrentHP < 0) enemyCurrentHP = 0;

                        UpdateUI();

                        Debug.Log($"[공격] 적에게 {cardUI.cardData.value} 데미지를 주었습니다!");
                    }
                    else if (cardUI.cardData.cardType == CardType.Heal)
                    {
                        Debug.Log(
                            $"[힐] {cardUI.cardData.cardName} → " +
                            $"{cardUI.cardData.value}만큼 회복!"
                        );
                    }
                    else if (cardUI.cardData.cardType == CardType.Debuff)
                    {
                        Debug.Log(
                            $"[디버프] {cardUI.cardData.cardName} → " +
                            $"적에게 {cardUI.cardData.statusEffect}!"
                        );
                    }

                    // 카드를 사용했으므로 GameObject 파괴
                    Destroy(cardUI.gameObject);
                    yield return new WaitForSeconds(0.5f); // 공격 연출 간격 대기
                }
            }
        }

        yield return new WaitForSeconds(0.5f);

        // 2. 적의 반격 (적이 살아있는 경우)
        if (enemyCurrentHP > 0)
        {
            playerCurrentHP -= enemyAttackDamage;
            if (playerCurrentHP < 0) playerCurrentHP = 0;
            UpdateUI();
            Debug.Log($"[적 반격] 플레이어가 {enemyAttackDamage} 데미지를 받았습니다!");
            yield return new WaitForSeconds(0.5f);
        }

        // 3. 기존 GameManager의 StartTurn()을 호출하여 부족한 손패(7장) 재보충
        if (gameManager != null)
        {
            gameManager.StartTurn();
        }

        isAttacking = false;
    }

    private void UpdateUI()
    {
        if (enemyHPText != null)
            enemyHPText.text = $"{enemyName} HP: {enemyCurrentHP}/{enemyMaxHP}";

        if (playerHPText != null)
            playerHPText.text = $"Player HP: {playerCurrentHP}/{playerMaxHP}";
    }
}