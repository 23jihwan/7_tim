using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 방 종류별 겉모습 (색, 아이콘)
[Serializable]
public class RoomVisual
{
    public RoomType type;
    public Color color = Color.white;
    public Sprite icon;   // 나중에 아이콘 그림이 생기면 여기에 넣는다
}

// MapGenerator로 만든 맵을 화면(Canvas)에 그리는 스크립트
public class MapView : MonoBehaviour
{
    [Header("맵 데이터")]
    public MapConfig config;

    [Header("화면 연결")]
    [Tooltip("노드와 연결선이 그려질 영역 (책 페이지 안쪽)")]
    public RectTransform mapContent;
    [Tooltip("MapNodeButton이 붙은 노드 프리팹")]
    public MapNodeButton nodePrefab;
    [Tooltip("노드를 눌렀을 때 뜨는 확인창 (비워 두면 바로 들어감)")]
    public MapConfirmPopup confirmPopup;
    [Tooltip("이벤트 노드에 들어가면 뜨는 이벤트 창")]
    public EventPopup eventPopup;   // ★ 이벤트 창

    [Header("배치")]
    public float padding = 80f;
    public float nodeSize = 56f;
    public float bossSize = 84f;
    public float lineWidth = 4f;

    [Header("모양")]
    public Color lineColor = new Color32(0x8A, 0x74, 0x54, 0xFF);
    public RoomVisual[] roomVisuals;

    [Header("진행 표시")]
    public Color walkedLineColor = new Color32(0x8F, 0x2A, 0x1E, 0xFF);   // 지나온 길 (붉은 잉크)
    public Color nextLineColor = new Color32(0x3A, 0x2C, 0x1A, 0xFF);     // 지금 갈 수 있는 길

    private MapData currentMap;
    private MapNode currentNode;                                    // 현재 위치 (null = 아직 출발 전)
    private readonly HashSet<MapNode> visited = new HashSet<MapNode>();

    private class LineInfo
    {
        public MapNode from;
        public MapNode to;
        public Image image;
    }
    private readonly List<LineInfo> lines = new List<LineInfo>();
    private RectTransform linesLayer;
    private RectTransform nodesLayer;
    private readonly Dictionary<MapNode, MapNodeButton> buttons = new Dictionary<MapNode, MapNodeButton>();
    private readonly Dictionary<Vector2Int, MapNode> nodeLookup = new Dictionary<Vector2Int, MapNode>();

    private void Reset()
    {
        roomVisuals = new RoomVisual[]
        {
            new RoomVisual { type = RoomType.Enemy, color = new Color32(0x7A, 0x64, 0x48, 0xFF) },
            new RoomVisual { type = RoomType.Elite, color = new Color32(0x8F, 0x2A, 0x1E, 0xFF) },
            new RoomVisual { type = RoomType.Event, color = new Color32(0x2F, 0x5E, 0x7A, 0xFF) },
            new RoomVisual { type = RoomType.Shop,  color = new Color32(0xB8, 0x86, 0x2B, 0xFF) },
            new RoomVisual { type = RoomType.Rest,  color = new Color32(0x4F, 0x7A, 0x3A, 0xFF) },
            new RoomVisual { type = RoomType.Boss,  color = new Color32(0x5B, 0x2A, 0x6E, 0xFF) }
        };
    }

    private void Start()
    {
        Build();
    }

    [ContextMenu("새 런 시작 (테스트용, Play 중에만)")]
    public void StartNewRunForTest()
    {
        if (!Application.isPlaying) return;
        RunState.GetOrCreate().NewRun();
        Build();
    }

    public void Build()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("MapView: Play 모드에서만 다시 그릴 수 있어요.");
            return;
        }
        if (config == null || mapContent == null || nodePrefab == null)
        {
            Debug.LogWarning("MapView: Config, Map Content, Node Prefab 칸을 모두 채워 주세요.");
            return;
        }

        RunState run = RunState.GetOrCreate();
        run.config = config;   // 층별 몬스터 정보를 RunState도 쓸 수 있게
        run.EnsureRunStarted();
        currentMap = MapGenerator.Generate(config, run.floorIndex, run.seed);

        PrepareLayers();
        Clear();

        // 연결선을 먼저 그리고, 노드를 그 위에 그린다
        foreach (List<MapNode> column in currentMap.columns)
            foreach (MapNode node in column)
                foreach (MapNode target in node.next)
                    CreateLine(node, target);

        foreach (List<MapNode> column in currentMap.columns)
            foreach (MapNode node in column)
                CreateNode(node);
        CreateNode(currentMap.boss);

        // 저장된 진행 상황 되살리기
        currentNode = null;
        visited.Clear();
        foreach (Vector2Int pos in run.visited)
            if (nodeLookup.TryGetValue(pos, out MapNode found)) visited.Add(found);
        if (run.hasPosition) nodeLookup.TryGetValue(run.currentPos, out currentNode);

        RefreshVisuals();

        Debug.Log($"{run.floorIndex + 1}층 맵 (시드 {run.seed}) · 체력 {run.playerHP}/{run.playerMaxHP} · 골드 {run.gold}");
    }

    private Vector2 GetPosition(MapNode node)
    {
        Rect rect = mapContent.rect;
        int steps = currentMap.columns.Count;           // 마지막 열 다음이 보스
        float stepX = (rect.width - padding * 2f) / steps;
        float laneGap = (rect.height - padding * 2f) / 2f;

        float x = rect.xMin + padding + node.column * stepX;
        float y = rect.center.y + (1 - node.lane) * laneGap;   // 위(0)가 화면 위쪽
        return new Vector2(x, y);
    }

    private void PrepareLayers()
    {
        if (linesLayer == null) linesLayer = CreateLayer("Lines");
        if (nodesLayer == null) nodesLayer = CreateLayer("Nodes");
        linesLayer.SetAsLastSibling();
        nodesLayer.SetAsLastSibling();
    }

    private RectTransform CreateLayer(string layerName)
    {
        GameObject go = new GameObject(layerName, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(mapContent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    private void Clear()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }

        if (linesLayer != null)
        {
            foreach (Transform child in linesLayer)
            {
                if (child != null && child.gameObject != null)
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
            }
        }

        if (nodesLayer != null)
        {
            foreach (Transform child in nodesLayer)
            {
                if (child != null && child.gameObject != null)
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
            }
        }

        buttons.Clear();
        lines.Clear();
        nodeLookup.Clear();
    }

    private void CreateLine(MapNode fromNode, MapNode toNode)
    {
        Vector2 from = GetPosition(fromNode);
        Vector2 to = GetPosition(toNode);

        GameObject go = new GameObject("Line", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(linesLayer, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

        Vector2 dir = to - from;
        rt.sizeDelta = new Vector2(dir.magnitude, lineWidth);
        rt.anchoredPosition = (from + to) / 2f;
        rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        Image image = go.GetComponent<Image>();
        image.color = lineColor;
        image.raycastTarget = false;   // 선은 클릭을 막지 않게

        lines.Add(new LineInfo { from = fromNode, to = toNode, image = image });
    }

    private void CreateNode(MapNode node)
    {
        MapNodeButton nodeButton = Instantiate(nodePrefab, nodesLayer);
        RectTransform rt = (RectTransform)nodeButton.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = GetPosition(node);

        RoomVisual visual = FindVisual(node.type);
        float size = node.type == RoomType.Boss ? bossSize : nodeSize;
        nodeButton.Setup(node, visual != null ? visual.color : Color.white, visual != null ? visual.icon : null, size);

        if (nodeButton.button != null)
            nodeButton.button.onClick.AddListener(() => OnNodeClicked(node));

        buttons[node] = nodeButton;
        nodeLookup[new Vector2Int(node.column, node.lane)] = node;
    }

    private bool IsReachable(MapNode node)
    {
        if (currentNode == null) return node.column == 0;
        return currentNode.next.Contains(node);
    }

    private void OnNodeClicked(MapNode node)
    {
        if (!IsReachable(node)) return;

        if (confirmPopup != null)
        {
            string title = node.type == RoomType.Boss ? "보스" : $"{node.column + 1}열 · {RoomName(node.type)}";
            confirmPopup.Show(title, RoomDescription(node.type) + "\n\n이 방으로 들어갈까요?", () => EnterNode(node));
        }
        else
        {
            EnterNode(node);
        }
    }

    private void EnterNode(MapNode node)
    {
        currentNode = node;
        visited.Add(node);
        RunState.Instance.MoveTo(node);
        RefreshVisuals();
        Debug.Log($"{node.column + 1}열 {MapGenerator.KoreanName(node.type)} 방으로 이동했어요.");

        if (!RunState.Instance.EnterRoom(node.type))
        {
            if (node.type == RoomType.Event && eventPopup != null)
                eventPopup.Open();
            else
                Debug.Log($"{MapGenerator.KoreanName(node.type)} 방은 아직 구현 전이라 바로 통과해요.");
        }
    }

    private void RefreshVisuals()
    {
        int currentColumn = currentNode == null ? -1 : currentNode.column;

        foreach (KeyValuePair<MapNode, MapNodeButton> pair in buttons)
        {
            MapNode node = pair.Key;
            NodeState state;
            if (node == currentNode) state = NodeState.Current;
            else if (visited.Contains(node)) state = NodeState.Visited;
            else if (IsReachable(node)) state = NodeState.Reachable;
            else if (node.column <= currentColumn) state = NodeState.Missed;
            else state = NodeState.Locked;

            pair.Value.SetState(state);
        }

        foreach (LineInfo line in lines)
        {
            bool walked = visited.Contains(line.from) && visited.Contains(line.to);
            bool next = line.from == currentNode;
            bool missed = !walked && line.to.column <= currentColumn;

            Color color = lineColor;
            float width = lineWidth;
            if (walked) { color = walkedLineColor; width = lineWidth * 1.8f; }
            else if (next) { color = nextLineColor; }
            else if (missed) { color.a = 0.3f; }

            line.image.color = color;
            RectTransform rt = line.image.rectTransform;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, width);
        }
    }

    private string RoomName(RoomType type)
    {
        switch (type)
        {
            case RoomType.Enemy: return "일반 몬스터";
            case RoomType.Elite: return "엘리트 몬스터";
            case RoomType.Event: return "이벤트";
            case RoomType.Shop: return "상점";
            case RoomType.Rest: return "휴식/강화";
            case RoomType.Boss: return "보스";
        }
        return "";
    }

    private string RoomDescription(RoomType type)
    {
        switch (type)
        {
            case RoomType.Enemy: return "일반 몬스터와 전투합니다.\n보상: 골드, 카드 선택";
            case RoomType.Elite: return "강한 엘리트 몬스터와 전투합니다.\n보상: 골드, 유물, 카드 선택";
            case RoomType.Event: return "선택에 따라 유물, 골드, 카드를 얻거나 대가를 치릅니다.";
            case RoomType.Shop: return "골드로 카드, 유물, 회복 아이템을 삽니다.";
            case RoomType.Rest: return "체력을 회복하거나 카드를 강화합니다.";
            case RoomType.Boss: return "이 층의 보스와 전투합니다.\n보상: 희귀 카드, 희귀 유물";
        }
        return "";
    }

    private RoomVisual FindVisual(RoomType type)
    {
        if (roomVisuals == null) return null;
        foreach (RoomVisual v in roomVisuals)
            if (v.type == type) return v;
        return null;
    }
}