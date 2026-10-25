using UnityEngine;
using UnityEngine.UI;

// 지도 옆에 "어떤 색·아이콘이 무슨 방인지" 알려주는 범례를 코드로 만든다
// 빈 오브젝트(RectTransform)에 붙이고 Map View만 연결하면 Play할 때 자동으로 채워진다
public class MapLegend : MonoBehaviour
{
    public MapView mapView;

    [Header("색")]
    public Color panelColor = new Color32(0xEA, 0xDF, 0xC3, 0xFF);
    public Color textColor = new Color32(0x3A, 0x2C, 0x1A, 0xFF);
    public Color subTextColor = new Color32(0x6B, 0x56, 0x36, 0xFF);

    private static readonly (RoomType type, string name, string desc)[] Entries =
    {
        (RoomType.Enemy, "일반 몬스터", "기본 전투 · 골드와 카드"),
        (RoomType.Elite, "엘리트 몬스터", "강한 적 · 유물 보상"),
        (RoomType.Event, "이벤트", "선택에 따라 달라지는 사건"),
        (RoomType.Shop,  "상점", "골드로 카드·유물 구매"),
        (RoomType.Rest,  "휴식/강화", "체력 회복 또는 카드 강화"),
        (RoomType.Boss,  "보스", "이 층의 마지막 전투"),
    };

    private static readonly string[] Tips =
    {
        "깜빡이는 방: 지금 갈 수 있는 곳",
        "흐린 방: 지나쳐서 갈 수 없는 곳",
        "붉은 선: 지나온 길",
    };

    private Font font;

    private void Start()
    {
        Build();
    }

    private void Build()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 배경 판
        Image bg = GetComponent<Image>();
        if (bg == null) bg = gameObject.AddComponent<Image>();
        bg.color = panelColor;
        bg.raycastTarget = false;
        Outline border = GetComponent<Outline>();
        if (border == null) border = gameObject.AddComponent<Outline>();
        border.effectColor = textColor;
        border.effectDistance = new Vector2(3, -3);

        float y = -28f;
        AddText("지도 보는 법", 30, FontStyle.Bold, textColor, TextAnchor.MiddleCenter, 0, y, 260, 40);
        y -= 64f;

        foreach (var e in Entries)
        {
            RoomVisual visual = FindVisual(e.type);
            AddDiamond(visual, -105f, y - 28f, e.type == RoomType.Boss ? 46f : 40f);
            AddText(e.name, 24, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, 30f, y, 200, 30);
            AddText(e.desc, 17, FontStyle.Normal, subTextColor, TextAnchor.MiddleLeft, 30f, y - 30f, 200, 26);
            y -= 76f;
        }

        // 구분선
        Image line = NewImage("Line", textColor);
        SetTop(line.rectTransform, 0, y + 6f, 240, 2);
        y -= 20f;

        foreach (string tip in Tips)
        {
            AddText(tip, 17, FontStyle.Normal, subTextColor, TextAnchor.MiddleLeft, 0, y, 250, 26);
            y -= 30f;
        }
    }

    // ---------------- 만들기 도우미 ----------------

    private RoomVisual FindVisual(RoomType type)
    {
        if (mapView == null || mapView.roomVisuals == null) return null;
        foreach (RoomVisual v in mapView.roomVisuals)
            if (v.type == type) return v;
        return null;
    }

    private void AddDiamond(RoomVisual visual, float x, float y, float size)
    {
        Image diamond = NewImage("Diamond", visual != null ? visual.color : Color.gray);
        SetTop(diamond.rectTransform, x, y, size, size);
        diamond.rectTransform.pivot = new Vector2(0.5f, 0.5f);      // 가운데를 기준으로 돌린다
        diamond.rectTransform.anchoredPosition = new Vector2(x, y);  // y = 마름모 중심
        diamond.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);

        if (visual != null && visual.icon != null)
        {
            GameObject go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(diamond.transform, false);
            Image icon = go.GetComponent<Image>();
            icon.sprite = visual.icon;
            icon.raycastTarget = false;
            icon.rectTransform.sizeDelta = Vector2.one * size * 0.62f;
            icon.rectTransform.localRotation = Quaternion.Euler(0, 0, -45);
        }
    }

    private Image NewImage(string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void AddText(string content, int size, FontStyle style, Color color, TextAnchor align, float x, float y, float w, float h)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(transform, false);
        Text text = go.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = align;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.raycastTarget = false;
        SetTop(text.rectTransform, x, y, w, h);
    }

    // 판 위쪽 가운데를 기준으로 배치한다
    private static void SetTop(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);
    }
}