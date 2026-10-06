using System.Collections.Generic;
using System.Text;
using UnityEngine;

// 맵의 방 하나
public class MapNode
{
    public int column;      // 몇 번째 열인지 (0부터)
    public int lane;        // 위(0) / 가운데(1) / 아래(2)
    public RoomType type = RoomType.Enemy;
    public List<MapNode> next = new List<MapNode>();   // 다음 열에서 갈 수 있는 방
    public List<MapNode> prev = new List<MapNode>();   // 이 방으로 들어오는 방
}

// 한 층의 맵 전체
public class MapData
{
    public int floorIndex;  // 0 = 1층
    public int seed;
    public List<List<MapNode>> columns = new List<List<MapNode>>();
    public MapNode boss;
}

// 기획서 3. 맵 랜덤 생성 규칙을 그대로 구현한 생성기
public static class MapGenerator
{
    private static readonly RoomType[] SpecialRooms = { RoomType.Elite, RoomType.Shop, RoomType.Rest };

    public static MapData Generate(MapConfig config, int floorIndex, int seed)
    {
        FloorSettings floor = config.floors[floorIndex];
        System.Random rng = new System.Random(seed * 31 + floorIndex * 7919);
        MapData map = new MapData { floorIndex = floorIndex, seed = seed };
        int n = floor.columnCount;

        // 1. 각 열에 방 2~3개 배치 (첫 열은 항상 3개)
        for (int c = 0; c < n; c++)
        {
            List<int> lanes = new List<int> { 0, 1, 2 };
            if (c != 0 && rng.NextDouble() >= 0.45)
                lanes.RemoveAt(rng.Next(3));

            List<MapNode> column = new List<MapNode>();
            foreach (int lane in lanes)
                column.Add(new MapNode { column = c, lane = lane });
            map.columns.Add(column);
        }

        // 2. 열과 열 사이 연결
        for (int c = 0; c < n - 1; c++)
            ConnectColumns(map.columns[c], map.columns[c + 1], rng);

        // 3. 보스 방: 마지막 열의 모든 방과 연결
        map.boss = new MapNode { column = n, lane = 1, type = RoomType.Boss };
        foreach (MapNode node in map.columns[n - 1])
        {
            node.next.Add(map.boss);
            map.boss.prev.Add(node);
        }

        // 4. 방 종류 정하기
        AssignTypes(map, floor, rng);
        return map;
    }

    // ---------------- 연결 ----------------

    private static void ConnectColumns(List<MapNode> from, List<MapNode> to, System.Random rng)
    {
        List<(MapNode a, MapNode b)> result = null;

        for (int attempt = 0; attempt < 30 && result == null; attempt++)
        {
            // 같은 레인 또는 바로 옆 레인끼리만 연결 후보
            List<(MapNode a, MapNode b)> edges = new List<(MapNode a, MapNode b)>();
            foreach (MapNode a in from)
                foreach (MapNode b in to)
                    if (Mathf.Abs(a.lane - b.lane) <= 1) edges.Add((a, b));
            Shuffle(edges, rng);

            // 서로 교차하는 연결선 제거
            int i, j;
            while (FindCrossing(edges, out i, out j))
                edges.RemoveAt(rng.Next(2) == 0 ? i : j);

            // 한 방에서 나가는 길은 최대 2개
            foreach (MapNode a in from)
            {
                while (CountOut(edges, a) > 2)
                {
                    List<(MapNode a, MapNode b)> mine = edges.FindAll(e => e.a == a);
                    edges.Remove(mine[rng.Next(mine.Count)]);
                }
            }

            // 길을 조금씩 랜덤으로 줄여서 매번 다른 모양이 되게 함
            List<(MapNode a, MapNode b)> copy = new List<(MapNode a, MapNode b)>(edges);
            Shuffle(copy, rng);
            foreach ((MapNode a, MapNode b) e in copy)
                if (rng.NextDouble() < 0.4 && CountOut(edges, e.a) > 1 && CountIn(edges, e.b) > 1)
                    edges.Remove(e);

            // 모든 방에 들어오는 길과 나가는 길이 최소 1개씩 있는지 검사
            bool ok = from.TrueForAll(a => CountOut(edges, a) >= 1) && to.TrueForAll(b => CountIn(edges, b) >= 1);
            if (ok) result = edges;
        }

        // 혹시 30번 다 실패하면 가장 가까운 방끼리 연결
        if (result == null)
        {
            result = new List<(MapNode a, MapNode b)>();
            foreach (MapNode a in from) result.Add((a, Nearest(to, a.lane)));
            foreach (MapNode b in to)
                if (!result.Exists(e => e.b == b)) result.Add((Nearest(from, b.lane), b));
        }

        foreach ((MapNode a, MapNode b) e in result)
        {
            e.a.next.Add(e.b);
            e.b.prev.Add(e.a);
        }
    }

    private static bool FindCrossing(List<(MapNode a, MapNode b)> edges, out int i, out int j)
    {
        for (i = 0; i < edges.Count; i++)
            for (j = i + 1; j < edges.Count; j++)
                if ((edges[i].a.lane - edges[j].a.lane) * (edges[i].b.lane - edges[j].b.lane) < 0)
                    return true;
        i = j = -1;
        return false;
    }

    private static int CountOut(List<(MapNode a, MapNode b)> edges, MapNode node) => edges.FindAll(e => e.a == node).Count;
    private static int CountIn(List<(MapNode a, MapNode b)> edges, MapNode node) => edges.FindAll(e => e.b == node).Count;

    private static MapNode Nearest(List<MapNode> column, int lane)
    {
        MapNode best = column[0];
        foreach (MapNode node in column)
            if (Mathf.Abs(node.lane - lane) < Mathf.Abs(best.lane - lane)) best = node;
        return best;
    }

    // ---------------- 방 종류 ----------------

    private static void AssignTypes(MapData map, FloorSettings floor, System.Random rng)
    {
        int n = floor.columnCount;
        int shopCount = 0;

        for (int c = 0; c < n; c++)
        {
            List<MapNode> column = map.columns[c];

            // 고정 방: 첫 열은 일반 몬스터, 보스 앞 열은 휴식/강화
            if (c == 0) { column.ForEach(node => node.type = RoomType.Enemy); continue; }
            if (c == n - 1) { column.ForEach(node => node.type = RoomType.Rest); continue; }

            List<MapNode> order = new List<MapNode>(column);
            Shuffle(order, rng);
            HashSet<RoomType> usedInColumn = new HashSet<RoomType>();

            // 고정 방: 중간 열에 상점 최소 1개
            MapNode fixedShop = (c == floor.shopColumn) ? order[0] : null;

            foreach (MapNode node in order)
            {
                if (node == fixedShop)
                {
                    node.type = RoomType.Shop;
                    usedInColumn.Add(RoomType.Shop);
                    shopCount++;
                    continue;
                }

                RoomType picked = RoomType.Enemy;
                for (int tries = 0; tries < 40; tries++)
                {
                    RoomType candidate = PickByWeight(floor.roomChances, rng);
                    bool special = System.Array.IndexOf(SpecialRooms, candidate) >= 0;

                    // 등장 조건 검사 (기획서 3. 등장 조건)
                    if (candidate == RoomType.Elite && c < floor.eliteStartColumn) continue;
                    if (candidate == RoomType.Rest && (c < floor.restStartColumn || c == n - 2)) continue;
                    if (candidate == RoomType.Shop && (c == floor.shopColumn - 1 || c == floor.shopColumn)) continue;
                    if (candidate == RoomType.Shop && c < floor.shopStartColumn) continue;
                    // 고정 상점 자리를 남겨 두고 최대 개수를 넘지 않게
                    int reserved = (c < floor.shopColumn) ? 1 : 0;
                    if (candidate == RoomType.Shop && shopCount + reserved >= floor.maxShops) continue;
                    if (special && usedInColumn.Contains(candidate)) continue;
                    if (special && node.prev.Exists(p => p.type == candidate)) continue;

                    picked = candidate;
                    break;
                }

                node.type = picked;
                usedInColumn.Add(picked);
                if (picked == RoomType.Shop) shopCount++;
            }
        }

        // 엘리트 최소 개수 보장
        EnsureMinElites(map, floor, rng);
    }

    private static void EnsureMinElites(MapData map, FloorSettings floor, System.Random rng)
    {
        int n = floor.columnCount;
        int eliteCount = 0;
        foreach (List<MapNode> column in map.columns)
            eliteCount += column.FindAll(node => node.type == RoomType.Elite).Count;

        while (eliteCount < floor.minElites)
        {
            // 엘리트로 바꿔도 등장 조건을 지키는 일반/이벤트 방 찾기
            List<MapNode> candidates = new List<MapNode>();
            for (int c = Mathf.Max(1, floor.eliteStartColumn); c < n - 1; c++)
            {
                List<MapNode> column = map.columns[c];
                if (column.Exists(node => node.type == RoomType.Elite)) continue;

                foreach (MapNode node in column)
                {
                    bool replaceable = node.type == RoomType.Enemy || node.type == RoomType.Event;
                    bool nearElite = node.prev.Exists(p => p.type == RoomType.Elite) || node.next.Exists(x => x.type == RoomType.Elite);
                    if (replaceable && !nearElite) candidates.Add(node);
                }
            }

            if (candidates.Count == 0) break;   // 바꿀 수 있는 방이 없으면 포기
            candidates[rng.Next(candidates.Count)].type = RoomType.Elite;
            eliteCount++;
        }
    }

    private static RoomType PickByWeight(RoomChance[] chances, System.Random rng)
    {
        int total = 0;
        foreach (RoomChance chance in chances) total += chance.weight;
        if (total <= 0) return RoomType.Enemy;

        int roll = rng.Next(total);
        foreach (RoomChance chance in chances)
        {
            if (roll < chance.weight) return chance.type;
            roll -= chance.weight;
        }
        return RoomType.Enemy;
    }

    private static void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int k = rng.Next(i + 1);
            (list[i], list[k]) = (list[k], list[i]);
        }
    }

    // ---------------- 테스트용 출력 ----------------

    public static string KoreanName(RoomType type)
    {
        switch (type)
        {
            case RoomType.Enemy: return "일반";
            case RoomType.Elite: return "엘리트";
            case RoomType.Event: return "이벤트";
            case RoomType.Shop: return "상점";
            case RoomType.Rest: return "휴식";
            case RoomType.Boss: return "보스";
        }
        return type.ToString();
    }

    public static string ToDebugString(MapData map)
    {
        string[] laneNames = { "위", "중", "아래" };
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"=== {map.floorIndex + 1}층 맵 (시드 {map.seed}) ===");

        for (int c = 0; c < map.columns.Count; c++)
        {
            sb.Append($"{c + 1}열: ");
            foreach (MapNode node in map.columns[c])
            {
                List<string> targets = node.next.ConvertAll(t => t.type == RoomType.Boss ? "보스" : laneNames[t.lane]);
                sb.Append($"[{laneNames[node.lane]}]{KoreanName(node.type)} → {string.Join(",", targets)}   ");
            }
            sb.AppendLine();
        }
        sb.AppendLine("보스");
        return sb.ToString();
    }
}