using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 씬이 바뀌어도 사라지지 않고 진행 상황을 기억하는 스크립트
// 맵 씬에서 자동으로 만들어지므로 씬에 직접 놓을 필요는 없다
public class RunState : MonoBehaviour
{
    public static RunState Instance { get; private set; }

    [Header("씬 이름 (Build Profiles의 Scene List에 있어야 함)")]
    public string mapSceneName = "minhyeokMapTestScene";
    public string battleSceneName = "JihwanTestScene";

    [Header("진행 상태")]
    public int floorIndex = 0;            // 0 = 1층
    public int seed = 0;                  // 0 = 아직 런을 시작하지 않음
    public bool hasPosition = false;      // false = 아직 1열을 고르기 전
    public Vector2Int currentPos;         // x = 열, y = 레인
    public List<Vector2Int> visited = new List<Vector2Int>();
    public RoomType currentRoomType;

    [Header("플레이어")]
    public int playerMaxHP = 100;
    public int playerHP = 100;
    public int gold = 0;

    [Header("엘리트/보스 적 체력 배율")]
    public float eliteHpMultiplier = 1.5f;
    public float bossHpMultiplier = 2.5f;

    public const int FloorCount = 3;

    private AttackManager battle;
    private bool battleReady;
    private bool battleEnding;

    // 있으면 그대로 쓰고, 없으면 새로 만든다
    public static RunState GetOrCreate()
    {
        if (Instance == null)
            new GameObject("RunState").AddComponent<RunState>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ---------------- 런 관리 ----------------

    public void NewRun()
    {
        floorIndex = 0;
        seed = Random.Range(1000, 10000);
        hasPosition = false;
        visited.Clear();
        playerHP = playerMaxHP;
        gold = 0;
        Debug.Log($"새 런을 시작해요. (시드 {seed})");
    }

    public void EnsureRunStarted()
    {
        if (seed == 0) NewRun();
    }

    public void MoveTo(MapNode node)
    {
        currentPos = new Vector2Int(node.column, node.lane);
        hasPosition = true;
        visited.Add(currentPos);
        currentRoomType = node.type;
    }

    // 전투 방이면 전투 씬으로 이동하고 true를 돌려준다
    public bool EnterRoom(RoomType type)
    {
        if (type == RoomType.Enemy || type == RoomType.Elite || type == RoomType.Boss)
        {
            SceneManager.LoadScene(battleSceneName);
            return true;
        }
        return false;   // 상점, 이벤트, 휴식은 아직 구현 전
    }

    // ---------------- 전투 연결 ----------------
    // 전투 담당의 AttackManager 코드는 고치지 않고, 여기서 값을 넣고 결과를 지켜본다

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        battle = null;
        if (scene.name != battleSceneName) return;

        battle = FindFirstObjectByType<AttackManager>();
        if (battle == null)
        {
            Debug.LogWarning("RunState: 전투 씬에서 AttackManager를 찾지 못했어요.");
            return;
        }

        battleReady = false;
        battleEnding = false;

        // AttackManager의 Start()가 실행되기 전에 값을 바꿔 둔다
        battle.playerMaxHP = playerMaxHP;
        float multiplier = 1f;
        if (currentRoomType == RoomType.Elite) multiplier = eliteHpMultiplier;
        if (currentRoomType == RoomType.Boss) multiplier = bossHpMultiplier;
        battle.enemyMaxHP = Mathf.RoundToInt(battle.enemyMaxHP * multiplier);

        StartCoroutine(SetupBattleNextFrame());
    }

    private IEnumerator SetupBattleNextFrame()
    {
        // AttackManager의 Start()가 체력을 최대치로 채운 뒤에, 남아 있던 체력으로 바꾼다
        yield return null;
        if (battle == null) yield break;

        battle.playerCurrentHP = playerHP;
        if (battle.playerHPText != null)
            battle.playerHPText.text = $"Player HP: {playerHP}/{playerMaxHP}";

        battleReady = true;
        Debug.Log($"{floorIndex + 1}층 {MapGenerator.KoreanName(currentRoomType)} 전투 시작 (적 체력 {battle.enemyMaxHP})");
    }

    private void Update()
    {
        if (battle == null || !battleReady || battleEnding) return;

        if (battle.enemyCurrentHP <= 0) StartCoroutine(EndBattle(true));
        else if (battle.playerCurrentHP <= 0) StartCoroutine(EndBattle(false));
    }

    private IEnumerator EndBattle(bool won)
    {
        battleEnding = true;
        yield return new WaitForSeconds(1.5f);   // 결과를 잠깐 보여주고 넘어간다

        playerHP = Mathf.Max(0, battle.playerCurrentHP);

        if (won)
        {
            int reward = RollGold(currentRoomType);
            gold += reward;
            Debug.Log($"전투 승리! 골드 +{reward} (보유 {gold}, 체력 {playerHP}/{playerMaxHP})");

            if (currentRoomType == RoomType.Boss) ClearFloor();
        }
        else
        {
            Debug.Log("게임 오버! 새 런을 시작해요.");
            NewRun();
        }

        battle = null;
        SceneManager.LoadScene(mapSceneName);
    }

    private void ClearFloor()
    {
        floorIndex++;
        if (floorIndex >= FloorCount)
        {
            Debug.Log("최종 보스 처치! 탑을 정복했어요. (엔딩은 나중에 구현)");
            NewRun();
            return;
        }

        seed = Random.Range(1000, 10000);
        hasPosition = false;
        visited.Clear();
        Debug.Log($"{floorIndex}층 클리어! {floorIndex + 1}층으로 올라가요.");
    }

    // 기획서 4. 골드 획득량 (7단계에서 보상 시스템으로 옮길 예정)
    private int RollGold(RoomType type)
    {
        int[,] enemy = { { 15, 25 }, { 25, 35 }, { 35, 45 } };
        int[,] elite = { { 30, 45 }, { 45, 60 }, { 60, 80 } };
        int[] boss = { 80, 120, 0 };
        int f = Mathf.Clamp(floorIndex, 0, 2);

        switch (type)
        {
            case RoomType.Enemy: return Random.Range(enemy[f, 0], enemy[f, 1] + 1);
            case RoomType.Elite: return Random.Range(elite[f, 0], elite[f, 1] + 1);
            case RoomType.Boss: return boss[f];
        }
        return 0;
    }
}