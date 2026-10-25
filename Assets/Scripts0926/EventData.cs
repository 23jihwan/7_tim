using System;
using UnityEngine;

// 이벤트 보상 종류 (기획: 1 유물, 2 골드, 3 카드, 4 1회용 아이템은 나중에)
public enum EventRewardType { None, Relic, Gold, Card, Item }

[Serializable]
public class EventChoice
{
    [Tooltip("버튼에 보일 글. 예: 골드 30을 주고 돕는다")]
    public string text;

    [Header("대가")]
    public int goldCost;
    public int hpCost;

    [Header("보상")]
    public EventRewardType reward = EventRewardType.None;
    [Tooltip("골드 보상 범위")]
    public int goldMin = 20;
    public int goldMax = 40;
    [Tooltip("특정 유물을 줄 때. 비워 두면 유물 후보 중 무작위")]
    public RelicData relic;

    [Header("함정 (선택)")]
    [Range(0f, 1f)] public float trapChance;
    public int trapHpLoss = 10;

    [Tooltip("고른 뒤 보여줄 결과 문장")]
    [TextArea(2, 4)] public string resultText;
}

// Project 창 우클릭 → Create → 마법사의 탑 → Event Data
[CreateAssetMenu(fileName = "NewEvent", menuName = "마법사의 탑/Event Data")]
public class EventData : ScriptableObject
{
    public string title;
    public Sprite image;
    [TextArea(3, 6)] public string story;
    [Tooltip("선택지는 최대 3개. 마지막에 '지나간다'를 넣어 두는 걸 추천")]
    public EventChoice[] choices;
}