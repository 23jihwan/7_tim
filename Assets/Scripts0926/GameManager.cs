using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("UI Panels")]
    public Transform handPanel;       // HandPanelZone 스크립트 붙은 패널
    public Transform[] dropSlots;     // CardSlot 스크립트가 붙은 3개의 상단 슬롯

    [Header("Card Prefab")]
    public GameObject cardPrefab;

    [Header("Card ScriptableObjects (예시 3종)")]
    public List<CardData> cardDatabase = new List<CardData>();

    private const int MAX_HAND_SIZE = 7;

    private void Start()
    {
        StartTurn();
    }

    public void StartTurn()
    {
        int currentHandCount = handPanel.childCount;
        int cardsToDraw = MAX_HAND_SIZE - currentHandCount;

        for (int i = 0; i < cardsToDraw; i++)
        {
            if (cardDatabase.Count == 0) break;

            GameObject newCardObj = Instantiate(cardPrefab, handPanel);
            CardUI card = newCardObj.GetComponent<CardUI>();

            if (card != null)
            {
                // 3종류의 CardData 중 하나를 무작위로 선택하여 적용
                CardData randomData = cardDatabase[Random.Range(0, cardDatabase.Count)];
                card.SetupCard(randomData);
            }
        }
    }
}