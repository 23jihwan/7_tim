using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    public int floorIndex = 0;              // 0 = 1층
    public int seed = 0;                    // 0 = 아직 런을 시작하지 않음
    public bool hasPosition = false;        // false = 아직 1열을 고르기 전
    public Vector2Int currentPos;           // x = 열, y = 레인
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
    private EnemyData pendingEnemy;         // 이번 전투에 나올 몬스터
    private bool battleReady;
    private bool battleEnding;

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

    // ---------------- 유물 보너스 계산 메서드 ----------------

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

        playerMaxHP = 100 + GetTotalRelicBonus(StatBonusType.MaxHP);
        playerHP = playerMaxHP;

        gold = 0;
        obtainedCards.Clear();
        hasPendingReward = false;
        deck.Clear();
        if (config != null && config.startingDeck != null)
            foreach (CardData c in config.startingDeck)
                if (c != null) { deck.Add(c); RegisterInBook(c); }
        ownedRelics.Clear();
        for (int int_i = 0; int_i < equippedRelics.Length; int_i++) equippedRelics[int_i] = null;
        startingRelicsGiven = false;
        Debug.Log($"새 런을 시작해요. (시드 {seed})");
    }

    public void AddCard(CardData card)
    {
        if (card == null) return;
        deck.Add(card);
        obtainedCards.Add(card);
        RegisterInBook(card);
        Debug.Log($"덱에 {card.cardName} 추가! (덱 {deck.Count}장)");
    }

    private void RegisterInBook(CardData card)
    {
        if (card != null && EncyclopediaManager.Instance != null)
            EncyclopediaManager.Instance.UnlockCard(card.cardName);
    }

    public bool AddRelic(RelicData relic)
    {
        if (relic == null || ownedRelics.Contains(relic)) return false;
        ownedRelics.Add(relic);

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

    public bool EnterRoom(RoomType type)
    {
        if (type == RoomType.Shop && !string.IsNullOrEmpty(shopSceneName))
        {
            SceneManager.LoadScene(shopSceneName);
            return true;
        }

        if (type != RoomType.Enemy && type != RoomType.Elite && type != RoomType.Boss)
            return false;

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

        if (deck.Count == 0)
        {
            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm != null && gm.cardDatabase != null)
                foreach (CardData c in gm.cardDatabase)
                    if (c != null) { deck.Add(c); RegisterInBook(c); }
            Debug.Log($"시작 덱 {deck.Count}장으로 전투를 시작해요.");
        }

        battle.playerMaxHP = playerMaxHP;

        if (pendingEnemy != null)
        {
            battle.currentEnemyData = pendingEnemy;
        }
        else if (battle.currentEnemyData == null)
        {
            float multiplier = 1f;
            if (currentRoomType == RoomType.Elite) multiplier = eliteHpMultiplier;
            if (currentRoomType == RoomType.Boss) multiplier = bossHpMultiplier;
            battle.enemyMaxHP = Mathf.RoundToInt(battle.enemyMaxHP * multiplier);
        }

        StartCoroutine(SetupBattleNextFrame());
    }

    private IEnumerator SetupBattleNextFrame()
    {
        yield return null;
        if (battle == null) yield break;

        battle.playerCurrentHP = playerHP;
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
        yield return new WaitForSeconds(1f);

        playerHP = Mathf.Max(0, battle.playerCurrentHP);

        if (won)
        {
            int reward = RollGold(currentRoomType);
            gold += reward;
            Debug.Log($"전투 승리! 골드 +{reward} (보유 {gold}, 체력 {playerHP}/{playerMaxHP})");

            string title = "전투 승리!";
            if (currentRoomType == RoomType.Elite) title = "엘리트 처치!";
            if (currentRoomType == RoomType.Boss) title = $"{floorIndex + 1}층 보스 처치!";

            List<string> lines = new List<string>();
            lines.Add($"골드 +{reward}   (보유 {gold})");
            if (currentRoomType == RoomType.Elite || currentRoomType == RoomType.Boss)
                lines.Add("장비 1개 획득 (장비 시스템 준비 중)");
            lines.Add($"남은 체력 {playerHP} / {playerMaxHP}");

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
            yield return new WaitForSeconds(0.5f);
            Debug.Log("게임 오버! 새 런을 시작해요.");
            NewRun();
        }

        battle = null;
        SceneManager.LoadScene(mapSceneName);
    }

    private List<CardData> PickCardChoices(int count)
    {
        List<CardData> result = new List<CardData>();
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm == null || gm.cardDatabase == null) return result;
        if (gm.cardPrefab != null) BattleRewardPopup.cardPrefab = gm.cardPrefab;

        List<CardData> pool = new List<CardData>(gm.cardDatabase);
        while (result.Count < count && pool.Count > 0)
        {
            int r = Random.Range(0, pool.Count);
            if (pool[r] != null) result.Add(pool[r]); // ★ 오타 난 력() 줄은 지우고 이 줄만 남겨주세요!
            pool.RemoveAt(r);
        }
        return result;
    }

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
            Debug.Log("최종 보스 처치! 탑을 정복했어요.");
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

        return GetModifiedGold(baseGold);
    }
}