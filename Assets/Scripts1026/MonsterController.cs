using UnityEngine;

public class MonsterController : MonoBehaviour
{
    [Header("Monster Info")]
    public string monsterName = "스켈레톤 병사";
    public int currentHp = 30;
    public int maxHp = 30;

    // 몬스터가 처치되었을 때 호출하는 함수
    public void OnMonsterDead()
    {
        // 실제 골드는 전투 승리 보상(RunState)에서 들어온다. 여기서는 금화가 날아가는 연출만
        if (CurrencyHUD.Instance != null)
        {
            CurrencyHUD.Instance.PlayGoldFlyEffect(transform.position, 0, false);
        }

        Debug.Log($"{monsterName} 처치!");
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