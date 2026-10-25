using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    // 어디서든 접근할 수 있는 싱글톤 인스턴스
    public static GameManager Instance { get; private set; }

    [Header("UI Panels & Card System")]
    public Transform handPanel;       // HandPanelZone 스크립트 붙은 패널
    public Transform[] dropSlots;     // CardSlot 스크립트가 붙은 3개의 상단 슬롯
    public GameObject cardPrefab;
    public List<CardData> cardDatabase = new List<CardData>();
    private const int MAX_HAND_SIZE = 7;

    [Header("Player Stats")]
    public float playerMaxHP = 100f;
    public float playerCurrentHP = 100f;

    [Header("Enemy Stats")]
    public float enemyMaxHP = 30f;
    public float enemyCurrentHP = 30f;

    [Header("UI References")]
    [Tooltip("구체 체력바(PlayerHP_Orb)에 붙인 PlayerHPUI 스크립트 연결")]
    public PlayerHPUI playerHPUI;

    [Tooltip("적 체력 표시용 TMP_Text (EnemyHPText)")]
    public TMP_Text enemyHPText;

    [Tooltip("(선택 사항) 기존 텍스트 체력바 연결 - 안 쓸 경우 비워둬도 에러 없음")]
    public TMP_Text playerHPText;

    private void Awake()
    {
        // 싱글톤 세팅
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitGame();
        StartTurn(); // 기존 카드 뽑기 기능 실행
    }

    /// <summary>
    /// 게임 시작 시 체력 및 UI 초기화
    /// </summary>
    public void InitGame()
    {
        playerCurrentHP = playerMaxHP;
        enemyCurrentHP = enemyMaxHP;

        UpdatePlayerHPUI();
        UpdateEnemyHPUI();
    }

    /// <summary>
    /// 턴 시작 시 패에 카드를 뽑아주는 기존 기능
    /// </summary>
    public void StartTurn()
    {
        if (handPanel == null || cardPrefab == null || cardDatabase == null || cardDatabase.Count == 0)
        {
            Debug.LogWarning("[GameManager] handPanel, cardPrefab 또는 cardDatabase가 인스펙터에서 설정되지 않았습니다.");
            return;
        }

        int currentHandCount = handPanel.childCount;
        int cardsToDraw = MAX_HAND_SIZE - currentHandCount;

        for (int i = 0; i < cardsToDraw; i++)
        {
            GameObject newCardObj = Instantiate(cardPrefab, handPanel);
            CardUI card = newCardObj.GetComponent<CardUI>();

            if (card != null)
            {
                // 3종류의 CardData 중 하나를 무작위로 선택하여 적용
                // 런 진행 중이면 내 덱에서, 아니면(전투 씬만 따로 테스트할 때) 원래 카드 목록에서 뽑는다
                List<CardData> pool = (RunState.Instance != null && RunState.Instance.deck.Count > 0) ? RunState.Instance.deck : cardDatabase;
                CardData randomData = pool[Random.Range(0, pool.Count)];
                card.SetupCard(randomData);                  // 카드에 데이터 넣기 (원래 있던 줄)
                CardAnimator.PlayDraw(card, i * 0.08f);   // 한 장씩 0.08초 간격으로 날아오기
            }
        }
    }

    #region --- Player HP System ---

    /// <summary>
    /// 플레이어 체력 직접 지정
    /// </summary>
    public void SetPlayerHP(float hp)
    {
        playerCurrentHP = Mathf.Clamp(hp, 0, playerMaxHP);
        UpdatePlayerHPUI();
    }

    /// <summary>
    /// 플레이어 데미지 입히기
    /// </summary>
    public void TakePlayerDamage(float damage)
    {
        playerCurrentHP -= damage;
        playerCurrentHP = Mathf.Max(0, playerCurrentHP);

        UpdatePlayerHPUI();

        if (playerCurrentHP <= 0)
        {
            OnPlayerDefeated();
        }
    }

    /// <summary>
    /// 플레이어 체력 회복 (힐)
    /// </summary>
    public void HealPlayer(float amount)
    {
        playerCurrentHP += amount;
        playerCurrentHP = Mathf.Min(playerMaxHP, playerCurrentHP);

        UpdatePlayerHPUI();
    }

    /// <summary>
    /// 플레이어 UI 업데이트 (구체 체력바 & 기존 텍스트 호환)
    /// </summary>
    public void UpdatePlayerHPUI()
    {
        // 1. 신규 구체 체력바 UI 갱신
        if (playerHPUI != null)
        {
            playerHPUI.SetHP(playerCurrentHP, playerMaxHP);
        }

        // 2. 기존 텍스트 UI가 있다면 함께 갱신 (Null 예외 방지)
        if (playerHPText != null)
        {
            playerHPText.text = $"Player HP: {Mathf.RoundToInt(playerCurrentHP)}/{Mathf.RoundToInt(playerMaxHP)}";
        }
    }

    private void OnPlayerDefeated()
    {
        Debug.Log("[GameManager] 플레이어가 패배했습니다!");
        // TODO: 게임 오버 처리
    }

    #endregion

    #region --- Enemy HP System ---

    /// <summary>
    /// 적에게 데미지 입히기
    /// </summary>
    public void TakeEnemyDamage(float damage)
    {
        enemyCurrentHP -= damage;
        enemyCurrentHP = Mathf.Max(0, enemyCurrentHP);

        UpdateEnemyHPUI();

        if (enemyCurrentHP <= 0)
        {
            OnEnemyDefeated();
        }
    }

    /// <summary>
    /// 적 체력 회복
    /// </summary>
    public void HealEnemy(float amount)
    {
        enemyCurrentHP += amount;
        enemyCurrentHP = Mathf.Min(enemyMaxHP, enemyCurrentHP);

        UpdateEnemyHPUI();
    }

    /// <summary>
    /// 적 체력 UI 갱신
    /// </summary>
    public void UpdateEnemyHPUI()
    {
        if (enemyHPText != null)
        {
            enemyHPText.text = $"스켈레톤 병사 HP: {Mathf.RoundToInt(enemyCurrentHP)}/{Mathf.RoundToInt(enemyMaxHP)}";
        }
    }

    private void OnEnemyDefeated()
    {
        Debug.Log("[GameManager] 적을 처치했습니다!");
        // TODO: 승리 처리
    }

    #endregion
}