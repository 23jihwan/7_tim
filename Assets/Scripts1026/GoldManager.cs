using UnityEngine;

public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance;

    [Header("Player Currency")]
    public int currentGold = 500; // 시작 골드 (테스트용)

    private void Awake()
    {
        // 싱글톤 및 씬 전환 시 유지 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 넘어가도 HUD가 사라지지 않고 유지됨!
        }
        else
        {
            Destroy(gameObject); // 이미 존재한다면 중복 생성 방지
        }
    }

    public void AddGold(int amount)
    {
        currentGold += amount;
        Debug.Log($"골드 획득: +{amount} / 현재 골드: {currentGold}");
    }

    public bool UseGold(int amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            Debug.Log($"골드 사용: -{amount} / 잔여 골드: {currentGold}");
            return true;
        }
        else
        {
            Debug.Log("골드가 부족합니다.");
            return false;
        }
    }
}