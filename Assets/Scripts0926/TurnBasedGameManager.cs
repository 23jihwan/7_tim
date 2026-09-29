//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class TurnBasedGameManager : MonoBehaviour
//{
//    [Header("UI Prefabs & Parent")]
//    public GameObject cardPrefab; // 아까 만든 CardPrefab 넣는 칸
//    public Transform handPanelTransform; // Hierarchy의 HandPanel 넣는 칸

//    [Header("Deck & Cards")]
//    public List<CardData> deckList = new List<CardData>();
//    public List<CardData> playerHand = new List<CardData>();
//    public List<CardData> selectedCards = new List<CardData>();

//    [Header("Entity Stats")]
//    public int playerMaxHp = 100;
//    public int playerHp;
//    public int monsterMaxHp = 100;
//    public int monsterHp;

//    [Header("Monster Passive")]
//    public bool monsterHasStartDebuff = true; // 몬스터의 시작 디버프 패시브 유무

//    private void Start()
//    {
//        playerHp = playerMaxHp;
//        monsterHp = monsterMaxHp;

//        StartGame();
//    }

//    // 1 & 2. 게임 시작 시 디버프 처리 후 7장 드로우 및 턴 시작
//    public void StartGame()
//    {
//        // 규칙 2: 몬스터 패시브 디버프 적용 후 카드 드로우
//        if (monsterHasStartDebuff)
//        {
//            ApplyDebuffToPlayer("StartDebuff");
//            Debug.Log("몬스터의 패시브로 플레이어가 시작 디버프를 받았습니다.");
//        }

//        // 규칙 1: 7장의 랜덤 카드 받고 턴 시작
//        DrawCards(7);
//        Debug.Log("플레이어 턴 시작. 7장의 카드를 드로우했습니다.");
//    }

//    // 카드 드로우 메서드
//    public void DrawCards(int count)
//    {
//        for (int i = 0; i < count; i++)
//        {
//            if (deckList.Count > 0)
//            {
//                int randomIndex = Random.Range(0, deckList.Count);
//                CardData drawnCard = deckList[randomIndex];
//                playerHand.Add(drawnCard);

//                // UI 화면에 카드 프리팹 생성
//                if (cardPrefab != null && handPanelTransform != null)
//                {
//                    GameObject newCardObj = Instantiate(cardPrefab, handPanelTransform);
//                    CardUI cardUI = newCardObj.GetComponent<CardUI>();
//                    if (cardUI != null)
//                    {
//                        cardUI.Setup(drawnCard);
//                    }
//                }
//            }
//        }
//    }

//    // 규칙 2: 카드 조합 로직 예시
//    public void CombineCards(CardData cardA, CardData cardB, CardData resultCard)
//    {
//        if (playerHand.Contains(cardA) && playerHand.Contains(cardB))
//        {
//            playerHand.Remove(cardA);
//            playerHand.Remove(cardB);
//            playerHand.Add(resultCard);
//            Debug.Log($"{cardA.cardName}와(과) {cardB.cardName}를 조합하여 {resultCard.cardName}를 만들었습니다.");
//        }
//    }

//    // 규칙 3 & 4: 카드 선택 (최대 3장까지)
//    public void SelectCard(CardData card)
//    {
//        if (selectedCards.Count < 3 && playerHand.Contains(card))
//        {
//            selectedCards.Add(card);
//            Debug.Log($"카드 선택됨: {card.cardName} (현재 선택된 카드 수: {selectedCards.Count}/3)");
//        }
//    }

//    // 규칙 4: 턴 종료 버튼 클릭 시 실행
//    public void OnTurnEndButtonClicked()
//    {
//        if (selectedCards.Count != 3)
//        {
//            Debug.LogWarning("카드를 3장 선택해야 합니다!");
//            return;
//        }

//        StartCoroutine(ProcessTurnSequence());
//    }

//    // 턴 진행 시퀀스
//    private IEnumerator ProcessTurnSequence()
//    {
//        // 규칙 5: 플레이어가 선택한 카드 효과 발동
//        yield return StartCoroutine(ExecutePlayerCards());

//        // 규칙 6: 플레이어 카드 효과 종료 후 몬스터의 턴 진행
//        yield return StartCoroutine(ExecuteMonsterTurn());

//        // 규칙 7: 소모된 카드 수만큼 다시 받아 총 7장 유지
//        RefillHand();
//    }

//    // 규칙 5: 플레이어 카드 효과 처리
//    private IEnumerator ExecutePlayerCards()
//    {
//        Debug.Log("--- 플레이어 카드 효과 발동 ---");

//        foreach (CardData card in selectedCards)
//        {
//            playerHand.Remove(card);

//            // 카드 효과 적용 (기존 로직 동일)
//            // ... (공격/힐/디버프 처리) ...

//            yield return new WaitForSeconds(0.5f);
//        }

//        // 화면의 HandPanel 자식으로 있는 CardUI들 중 사용된 카드 오브젝트 삭제
//        foreach (Transform child in handPanelTransform)
//        {
//            CardUI cardUI = child.GetComponent<CardUI>();
//            if (cardUI != null && cardUI.isSelected)
//            {
//                Destroy(child.gameObject);
//            }
//        }

//        selectedCards.Clear();
//    }

//    // 규칙 6: 몬스터 턴 행동
//    private IEnumerator ExecuteMonsterTurn()
//    {
//        Debug.Log("--- 몬스터 턴 ---");
//        yield return new WaitForSeconds(1f);

//        // 몬스터의 행동 (공격 또는 디버프)
//        int actionType = Random.Range(0, 2);
//        if (actionType == 0)
//        {
//            int damage = 10;
//            playerHp -= damage;
//            Debug.Log($"몬스터가 플레이어를 공격했습니다! {damage} 데미지 (남은 HP: {playerHp})");
//        }
//        else
//        {
//            ApplyDebuffToPlayer("MonsterDebuff");
//            Debug.Log("몬스터가 플레이어에게 방해 디버프를 걸었습니다.");
//        }

//        yield return new WaitForSeconds(1f);
//    }

//    // 규칙 7: 핸드가 7장이 되도록 보충 후 다음 턴 시작
//    private void RefillHand()
//    {
//        int drawAmount = 7 - playerHand.Count;
//        if (drawAmount > 0)
//        {
//            DrawCards(drawAmount);
//            Debug.Log($"카드를 {drawAmount}장 보충하여 총 {playerHand.Count}장이 되었습니다.");
//        }
//        Debug.Log("--- 새로운 플레이어 턴 시작 ---");
//    }

//    private void ApplyDebuffToPlayer(string debuffType)
//    {
//        // 디버프 적용 로직 구현
//    }
//}