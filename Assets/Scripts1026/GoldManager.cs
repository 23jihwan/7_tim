using UnityEngine;

// 골드를 쓰고 얻는 창구. 실제 골드는 RunState에 있어서
// 전투 보상, 이벤트, 상점이 모두 같은 골드를 쓴다
public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance;

    [Header("테스트용")]
    [Tooltip("상점 씬만 따로 Play할 때 주는 골드. 맵에서 시작한 런에는 적용되지 않는다")]
    public int testGold = 500;

    // 예전처럼 GoldManager.Instance.currentGold 로 읽고 쓸 수 있다
    public int currentGold
    {
        get { return RunState.GetOrCreate().gold; }
        set { RunState.GetOrCreate().gold = Mathf.Max(0, value); }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);   // 이미 있으면 중복 생성 방지
            return;
        }
        Instance = this;

        // 맨 바깥 오브젝트일 때만 씬이 바뀌어도 유지 (자식이면 경고가 나므로 하지 않음)
        if (transform.parent == null) DontDestroyOnLoad(gameObject);

        // 런을 시작하지 않은 상태(상점 씬만 따로 테스트)면 테스트 골드를 준다
        RunState run = RunState.GetOrCreate();
        if (run.seed == 0 && run.gold == 0) run.gold = testGold;
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

        Debug.Log("골드가 부족합니다.");
        return false;
    }
}