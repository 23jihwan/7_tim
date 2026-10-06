using System;
using UnityEngine;

// 방 종류 (기획서 기준 6종)
public enum RoomType
{
    Enemy,  // 일반 몬스터
    Elite,  // 엘리트 몬스터
    Event,  // 이벤트
    Shop,   // 상점
    Rest,   // 휴식/강화
    Boss    // 보스
}

// 방 하나의 등장 확률 (가중치)
[Serializable]
public class RoomChance
{
    public RoomType type;
    [Range(0, 100)] public int weight;

    public RoomChance(RoomType type, int weight)
    {
        this.type = type;
        this.weight = weight;
    }
}

// 층 하나의 설정
[Serializable]
public class FloorSettings
{
    public string floorName;

    [Tooltip("보스를 제외한 열 개수")]
    [Min(3)] public int columnCount;

    [Tooltip("상점이 반드시 1개 이상 나오는 열 (0부터 셈: 4열 = 3)")]
    public int shopColumn;

    [Tooltip("엘리트가 나오기 시작하는 열 (0부터 셈: 3열 = 2)")]
    public int eliteStartColumn;

    [Tooltip("휴식/강화가 나오기 시작하는 열 (0부터 셈: 4열 = 3)")]
    public int restStartColumn = 3;

    [Tooltip("상점이 나오기 시작하는 열 (고정 상점 제외, 0부터 셈: 3열 = 2)")]
    public int shopStartColumn = 2;

    [Tooltip("한 층의 상점 최대 개수 (고정 상점 포함)")]
    [Min(1)] public int maxShops = 2;

    [Tooltip("한 층에 반드시 나오는 엘리트 최소 개수")]
    [Min(0)] public int minElites = 1;

    [Tooltip("고정 방을 제외한 방의 등장 확률(%)")]
    public RoomChance[] roomChances;
}

// 유니티 Project 창에서 우클릭 → Create → 마법사의 탑 → Map Config 로 만들 수 있다
[CreateAssetMenu(fileName = "MapConfig", menuName = "마법사의 탑/Map Config")]
public class MapConfig : ScriptableObject
{
    [Tooltip("탑의 층 수만큼 설정한다")]
    public FloorSettings[] floors;

    // 에셋을 처음 만들 때 기획서 수치로 자동으로 채워진다
    private void Reset()
    {
        floors = new FloorSettings[]
        {
            new FloorSettings
            {
                floorName = "마법사의 입문",
                columnCount = 7,
                shopColumn = 3,
                eliteStartColumn = 2,
                roomChances = new RoomChance[]
                {
                    new RoomChance(RoomType.Enemy, 55),
                    new RoomChance(RoomType.Event, 17),
                    new RoomChance(RoomType.Rest, 12),
                    new RoomChance(RoomType.Elite, 8),
                    new RoomChance(RoomType.Shop, 8)
                }
            },
            new FloorSettings
            {
                floorName = "마법의 확장",
                columnCount = 8,
                shopColumn = 3,
                eliteStartColumn = 1,
                roomChances = new RoomChance[]
                {
                    new RoomChance(RoomType.Enemy, 45),
                    new RoomChance(RoomType.Event, 22),
                    new RoomChance(RoomType.Rest, 13),
                    new RoomChance(RoomType.Elite, 12),
                    new RoomChance(RoomType.Shop, 8)
                }
            },
            new FloorSettings
            {
                floorName = "마법사의 영역",
                columnCount = 9,
                shopColumn = 4,
                eliteStartColumn = 1,
                roomChances = new RoomChance[]
                {
                    new RoomChance(RoomType.Enemy, 40),
                    new RoomChance(RoomType.Event, 22),
                    new RoomChance(RoomType.Rest, 14),
                    new RoomChance(RoomType.Elite, 16),
                    new RoomChance(RoomType.Shop, 8)
                }
            }
        };
    }
}