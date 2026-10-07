using System.Collections;
using UnityEngine;
using TMPro;

public class AttackManager : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;   // GameManager 연결
    public Transform[] dropSlots;     // CardSlot이 배치된 상단 슬롯 배열 (3개)

    [Header("Enemy Stats")]
    public string enemyName = "스켈레톤";
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

    // UI [턴 종료 / 공격] 버튼의 OnClick()에 연결
    public void OnAttackButtonClicked()
    {
        if (isAttacking) return;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        // --------------------------------------------------
        // 1. 플레이어 공격 (슬롯에 올려진 카드의 value만큼 데미지)
        // --------------------------------------------------
        foreach (Transform slot in dropSlots)
        {
            // 슬롯이 비어있으면 다음 슬롯으로 통과
            if (slot == null || slot.childCount == 0) continue;

            CardUI cardUI = slot.GetChild(0).GetComponent<CardUI>();

            if (cardUI != null && cardUI.cardData != null)
            {
                // [공격 카드] 몬스터 HP 차감
                if (cardUI.cardData.cardType == CardType.Attack)
                {
                    enemyCurrentHP -= cardUI.cardData.value;
                    if (enemyCurrentHP < 0) enemyCurrentHP = 0;

                    UpdateUI();
                    Debug.Log($"[플레이어 공격] {enemyName}에게 {cardUI.cardData.value} 데미지! (남은 몬스터 HP: {enemyCurrentHP})");
                }
                // [힐 카드] 플레이어 HP 회복
                else if (cardUI.cardData.cardType == CardType.Heal)
                {
                    playerCurrentHP = Mathf.Min(playerCurrentHP + cardUI.cardData.value, playerMaxHP);
                    UpdateUI();
                    Debug.Log($"[플레이어 회복] HP {cardUI.cardData.value} 회복! (현재 플레이어 HP: {playerCurrentHP})");
                }

                // 사용 완료한 카드 파괴 및 연출 대기
                Destroy(cardUI.gameObject);
                yield return new WaitForSeconds(0.5f);

                // 공격 도중 몬스터가 사망하면 즉시 턴 종료 및 승리 처리
                if (enemyCurrentHP <= 0)
                {
                    Debug.Log($"★ 전투 승리! {enemyName}을(를) 처치했습니다.");
                    isAttacking = false;
                    yield break;
                }
            }
        }

        yield return new WaitForSeconds(0.3f);

        // --------------------------------------------------
        // 2. 몬스터 반격 (몬스터가 살아있을 때만 실행)
        // --------------------------------------------------
        if (enemyCurrentHP > 0)
        {
            playerCurrentHP -= enemyAttackDamage;
            if (playerCurrentHP < 0) playerCurrentHP = 0;

            UpdateUI();
            Debug.Log($"[몬스터 반격] {enemyName}이(가) 플레이어에게 {enemyAttackDamage} 데미지! (남은 플레이어 HP: {playerCurrentHP})");

            yield return new WaitForSeconds(0.5f);

            // 플레이어 사망 시 게임 오버 처리
            if (playerCurrentHP <= 0)
            {
                Debug.Log("☠ 패배... 플레이어의 HP가 0이 되었습니다.");
                isAttacking = false;
                yield break;
            }
        }

        // --------------------------------------------------
        // 3. 턴 종료 후 부족한 손패 채우기 (기획서 규칙 반영)
        // --------------------------------------------------
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