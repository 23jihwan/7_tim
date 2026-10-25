using UnityEngine;

public class MonsterController : MonoBehaviour
{
    [Header("Monster Info")]
    public string monsterName = "스켈레톤 병사";
    public int minGold = 100;
    public int maxGold = 130;
    public int currentHp = 30;
    public int maxHp = 30;

    // 몬스터가 처치되었을 때 호출하는 함수
    public void OnMonsterDead()
    {
        int rewardGold = Random.Range(minGold, maxGold + 1);
        Debug.Log($"{monsterName} 처치! 보상 골드: {rewardGold}");

        // 상단 HUD의 날아가는 연출 호출
        if (CurrencyHUD.Instance != null)
        {
            CurrencyHUD.Instance.PlayGoldFlyEffect(transform.position, rewardGold);
        }
        else
        {
            // HUD가 없으면 데이터만 즉시 증가
            GoldManager.Instance.AddGold(rewardGold);
        }

        // 몬스터 오브젝트 파괴
        Destroy(gameObject);
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;

        if (currentHp <= 0)
        {
            // 몬스터 사망 처리 함수 호출
            OnMonsterDead();
        }
    }
}