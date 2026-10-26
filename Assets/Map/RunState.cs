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
    [Tooltip("엘리트 전투 씬 이름. 비워 두면 일반 전투 씬을 같이 쓴다")]
    public string eliteSceneName = "TestScene";
    [Tooltip("보스 전투 씬 이름. 비워 두면 일반 전투 씬을 같이 쓴다")]
    public string bossSceneName = "BossScene";
    [Tooltip("상점 씬 이름. 비워 두면 상점 방은 바로 통과")]
    public string shopSceneName = "Shop";

    [Tooltip("맵 설정 (MapView가 자동으로 넣어 준다). 층별 등장 몬스터를 여기서 가져온다")]
    public MapConfig config;

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

    [Header("덱 (전투에서 이 카드들 중에서 뽑는다)")]
    public List<CardData> deck = new List<CardData>();

    [Header("보상")]
    [Tooltip("보상으로 얻은 카드 (덱 저장 기능이 생기면 덱에 넣는다)")]
    public List<CardData> obtainedCards = new List<CardData>();
    public bool hasPendingReward = false;   // 맵에서 보여줄 보상이 남아 있는지
    public int pendingGold;
    public RoomType pendingRoomType;

    [Header("유물 (InventoryManager가 함께 쓴다)")]
    public List<RelicData> ownedRelics = new List<RelicData>();
    public RelicData[] equippedRelics = new RelicData[5];
    [HideInInspector] public bool startingRelicsGiven = false;

    [Header("엘리트/보스 적 체력 배율")]
    public float eliteHpMultiplier = 1.5f;
    public float bossHpMultiplier = 2.5f;

    public const int FloorCount = 3;

    private AttackManager battle;
    private EnemyData pendingEnemy;        // 이번 전투에 나올 몬스터
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

    // ---------------- 유물 보너스 계산 (팀원 코드) ----------------

    public int GetTotalRelicBonus(StatBonusType targetType)
    {
        int totalBonus = 0;
        foreach (var relic in ownedRelics)
        {
            if (relic != null && relic.statBonusType == targetType)
            {
                totalBonus += relic.value;
            }
        }
        return totalBonus;
    }

    public int GetModifiedGold(int baseGold)
    {
        int bonusGold = GetTotalRelicBonus(StatBonusType.GoldBonus);
        return baseGold + bonusGold;
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
        obtainedCards.Clear();
        hasPendingReward = false;
        deck.Clear();
        if (config != null && config.startingDeck != null)
            foreach (CardData c in config.startingDeck)
                if (c != null) { deck.Add(c); RegisterInBook(c); }
        ownedRelics.Clear();
        for (int i = 0; i < equippedRelics.Length; i++) equippedRelics[i] = null;   // 배열은 그대로 두고 비우기만
        startingRelicsGiven = false;

        playerMaxHP = 100 + GetTotalRelicBonus(StatBonusType.MaxHP);   // (팀원 코드) 유물 최대 체력 보너스
        playerHP = playerMaxHP;
        Debug.Log($"새 런을 시작해요. (시드 {seed})");
    }

    // 카드 얻기 (전투 보상, 이벤트 등): 덱에 들어간다
    public void AddCard(CardData card)
    {
        if (card == null) return;
        deck.Add(card);
        obtainedCards.Add(card);
        RegisterInBook(card);
        Debug.Log($"덱에 {card.cardName} 추가! (덱 {deck.Count}장)");
    }

    // 얻은 카드는 모두 도감에 등록 (이미 등록된 카드면 아무 일도 없음)
    private void RegisterInBook(CardData card)
    {
        if (card != null && EncyclopediaManager.Instance != null)
            EncyclopediaManager.Instance.UnlockCard(card.cardName);
    }

    // 유물 얻기 (이벤트, 엘리트, 상점 보상 등). 이미 가진 유물이면 false
    public bool AddRelic(RelicData relic)
    {
        if (relic == null || ownedRelics.Contains(relic)) return false;
        ownedRelics.Add(relic);

        // (팀원 코드) 최대 체력 유물은 얻는 즉시 적용
        if (relic.statBonusType == StatBonusType.MaxHP)
        {
            playerMaxHP += relic.value;
            playerHP += relic.value;
            Debug.Log($"[유물 효과] 최대 체력 증가 +{relic.value} (현재 최대 체력: {playerMaxHP})");
        }

        Debug.Log($"유물 획득: {relic.relicName} (보유 {ownedRelics.Count}개)");
        return true;
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

    // 전투 방이면 몬스터를 고르고 전투 씬으로 이동한 뒤 true를 돌려준다
    public bool EnterRoom(RoomType type)
    {
        // 상점 방: 상점 씬으로 이동 (나가기 버튼을 누르면 맵으로 돌아온다)
        if (type == RoomType.Shop && !string.IsNullOrEmpty(shopSceneName))
        {
            SceneManager.LoadScene(shopSceneName);
            return true;
        }

        if (type != RoomType.Enemy && type != RoomType.Elite && type != RoomType.Boss)
            return false;   // 상점, 이벤트, 휴식은 아직 구현 전

        pendingEnemy = PickEnemy(type);
        SceneManager.LoadScene(GetBattleScene(type));
        return true;
    }

    private string GetBattleScene(RoomType type)
    {
        if (type == RoomType.Elite && !string.IsNullOrEmpty(eliteSceneName)) return eliteSceneName;
        if (type == RoomType.Boss && !string.IsNullOrEmpty(bossSceneName)) return bossSceneName;
        return battleSceneName;
    }

    private bool IsBattleScene(string sceneName)
    {
        return sceneName == battleSceneName || sceneName == eliteSceneName || sceneName == bossSceneName;
    }

    // MapConfig에서 지금 층, 방 종류에 맞는 몬스터를 하나 고른다 (없으면 null)
    private EnemyData PickEnemy(RoomType type)
    {
        if (config == null || config.floors == null || floorIndex >= config.floors.Length) return null;
        FloorSettings floor = config.floors[floorIndex];

        EnemyData[] list = floor.normalEnemies;
        if (type == RoomType.Elite) list = floor.eliteEnemies;
        if (type == RoomType.Boss) list = floor.bossEnemies;

        if (list == null || list.Length == 0) return null;
        return list[Random.Range(0, list.Length)];
    }

    // ---------------- 전투 연결 ----------------
    // 전투 담당의 AttackManager 코드는 고치지 않고, 여기서 값을 넣고 결과를 지켜본다

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        battle = null;
        if (!IsBattleScene(scene.name)) return;

        battle = FindFirstObjectByType<AttackManager>();
        if (battle == null)
        {
            Debug.LogWarning("RunState: 전투 씬에서 AttackManager를 찾지 못했어요.");
            return;
        }

        battleReady = false;
        battleEnding = false;

        // 시작 덱이 비어 있으면, 처음 한 번은 전투 씬의 카드 목록으로 덱을 만든다
        if (deck.Count == 0)
        {
            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm != null && gm.cardDatabase != null)
                foreach (CardData c in gm.cardDatabase)
                    if (c != null) { deck.Add(c); RegisterInBook(c); }
            Debug.Log($"시작 덱 {deck.Count}장으로 전투를 시작해요.");
        }

        // AttackManager의 Start()가 실행되기 전에 값을 바꿔 둔다
        battle.playerMaxHP = playerMaxHP;

        if (pendingEnemy != null)
        {
            // MapConfig에서 고른 몬스터를 넣으면 AttackManager가 이름·체력·공격력·그림을 적용한다
            battle.currentEnemyData = pendingEnemy;
        }
        else if (battle.currentEnemyData == null)
        {
            // 몬스터 데이터가 아무 데도 없을 때만 예전처럼 체력 배율을 쓴다
            float multiplier = 1f;
            if (currentRoomType == RoomType.Elite) multiplier = eliteHpMultiplier;
            if (currentRoomType == RoomType.Boss) multiplier = bossHpMultiplier;
            battle.enemyMaxHP = Mathf.RoundToInt(battle.enemyMaxHP * multiplier);
        }
        // 씬에 이미 몬스터 데이터가 들어 있으면 그대로 둔다 (체력이 두 번 오르지 않게)

        StartCoroutine(SetupBattleNextFrame());
    }

    private IEnumerator SetupBattleNextFrame()
    {
        // AttackManager의 Start()가 체력을 최대치로 채운 뒤에, 남아 있던 체력으로 바꾼다
        yield return null;
        if (battle == null) yield break;

        battle.playerCurrentHP = playerHP;
        // GameManager도 체력을 관리하므로 같이 맞춰 준다
        if (GameManager.Instance != null)
        {
            GameManager.Instance.playerMaxHP = playerMaxHP;
            GameManager.Instance.playerCurrentHP = playerHP;
            GameManager.Instance.UpdatePlayerHPUI();
        }
        if (battle.playerHPText != null)
            battle.playerHPText.text = $"Player HP: {playerHP}/{playerMaxHP}";

        battleReady = true;
        Debug.Log($"{floorIndex + 1}층 {MapGenerator.KoreanName(currentRoomType)} 전투 시작: {battle.enemyName} (체력 {battle.enemyMaxHP}, 공격력 {battle.enemyAttackDamage})");
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
        yield return new WaitForSeconds(1f);   // 쓰러지는 모습을 잠깐 보여준다

        playerHP = Mathf.Max(0, battle.playerCurrentHP);

        if (won)
        {
            SoundManager.Play("victory");
            int reward = RollGold(currentRoomType);
            gold += reward;
            Debug.Log($"전투 승리! 골드 +{reward} (보유 {gold}, 체력 {playerHP}/{playerMaxHP})");

            // 보상 창에 보여줄 내용
            string title = "전투 승리!";
            if (currentRoomType == RoomType.Elite) title = "엘리트 처치!";
            if (currentRoomType == RoomType.Boss) title = $"{floorIndex + 1}층 보스 처치!";

            List<string> lines = new List<string>();
            lines.Add($"골드 +{reward}   (보유 {gold})");
            if (currentRoomType == RoomType.Elite || currentRoomType == RoomType.Boss)
                lines.Add("장비 1개 획득 (장비 시스템 준비 중)");
            lines.Add($"남은 체력 {playerHP} / {playerMaxHP}");

            // 확인을 누를 때까지 기다린다
            bool confirmed = false;
            BattleRewardPopup.Show(title, lines, PickCardChoices(3), picked =>
            {
                if (picked != null)
                {
                    AddCard(picked);
                }
                else
                {
                    Debug.Log("카드 보상을 건너뛰었어요.");
                }
                confirmed = true;
            });
            while (!confirmed) yield return null;

            if (currentRoomType == RoomType.Boss) ClearFloor();
        }
        else
        {
            SoundManager.Play("defeat");
            yield return new WaitForSeconds(0.5f);
            Debug.Log("게임 오버! 새 런을 시작해요.");
            NewRun();
        }

        battle = null;
        SceneManager.LoadScene(mapSceneName);
    }

    // 전투 씬 GameManager의 카드 목록에서 서로 다른 카드를 골라 보상 후보로 만든다
    private List<CardData> PickCardChoices(int count)
    {
        List<CardData> result = new List<CardData>();
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm == null || gm.cardDatabase == null) return result;
        if (gm.cardPrefab != null) BattleRewardPopup.cardPrefab = gm.cardPrefab;   // 보상 카드를 진짜 카드 모양으로

        List<CardData> pool = new List<CardData>(gm.cardDatabase);
        while (result.Count < count && pool.Count > 0)
        {
            int r = Random.Range(0, pool.Count);
            if (pool[r] != null) result.Add(pool[r]);
            pool.RemoveAt(r);
        }
        return result;
    }

    // ---------------- 테스트용 ----------------
    // Play 중 Hierarchy의 DontDestroyOnLoad → RunState를 선택하고 점 세 개 메뉴에서 실행

    [ContextMenu("테스트: 전투 승리")]
    private void DebugWin()
    {
        if (battle != null && !battleEnding) StartCoroutine(EndBattle(true));
        else Debug.LogWarning("전투 씬에서만 쓸 수 있어요.");
    }

    [ContextMenu("테스트: 전투 패배")]
    private void DebugLose()
    {
        if (battle != null && !battleEnding) StartCoroutine(EndBattle(false));
        else Debug.LogWarning("전투 씬에서만 쓸 수 있어요.");
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

        int baseGold = 0;
        switch (type)
        {
            case RoomType.Enemy: baseGold = Random.Range(enemy[f, 0], enemy[f, 1] + 1); break;
            case RoomType.Elite: baseGold = Random.Range(elite[f, 0], elite[f, 1] + 1); break;
            case RoomType.Boss: baseGold = boss[f]; break;
        }

        return GetModifiedGold(baseGold);   // (팀원 코드) 골드 보너스 유물 적용
    }
}
