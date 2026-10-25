using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 맵에서 이벤트 노드에 들어가면 이야기와 선택지를 보여주고, 고른 결과를 RunState에 적용한다
// 맵 씬의 빈 오브젝트에 붙이고 이벤트 목록과 보상 후보를 채워 두면 된다 (화면은 코드가 만든다)
public class EventPopup : MonoBehaviour
{
    [Header("이벤트 목록 (이 중 하나가 무작위로 나온다)")]
    public EventData[] events;

    [Header("보상 후보")]
    public RelicData[] relicPool;
    public CardData[] cardPool;

    private static readonly Color Parchment = new Color32(0xEA, 0xDF, 0xC3, 0xFF);
    private static readonly Color Ink = new Color32(0x3A, 0x2C, 0x1A, 0xFF);
    private static readonly Color InkLight = new Color32(0x5A, 0x46, 0x30, 0xFF);
    private static readonly Color Brown = new Color32(0x7A, 0x64, 0x48, 0xFF);

    private Font font;
    private GameObject root;
    private Image picture;
    private Text titleText;
    private Text storyText;
    private readonly Button[] choiceButtons = new Button[3];
    private readonly Text[] choiceTexts = new Text[3];
    private EventData current;
    private EventData last;

    private void Awake()
    {
        BuildUI();
        root.SetActive(false);
    }

    // ---------------- 열기 ----------------

    public void Open()
    {
        if (events == null || events.Length == 0)
        {
            Debug.LogWarning("EventPopup: 이벤트 목록이 비어 있어요.");
            return;
        }

        // 바로 전과 같은 이벤트는 가능하면 피한다
        current = events[Random.Range(0, events.Length)];
        if (events.Length > 1)
            for (int tries = 0; tries < 5 && current == last; tries++) current = events[Random.Range(0, events.Length)];
        last = current;

        titleText.text = current.title;
        storyText.text = current.story;
        picture.gameObject.SetActive(current.image != null);
        if (current.image != null) picture.sprite = current.image;

        RunState run = RunState.GetOrCreate();
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            bool has = current.choices != null && i < current.choices.Length;
            choiceButtons[i].gameObject.SetActive(has);
            if (!has) continue;

            EventChoice c = current.choices[i];
            bool canAfford = run.gold >= c.goldCost && run.playerHP > c.hpCost;
            choiceTexts[i].text = Label(c, canAfford);
            choiceButtons[i].interactable = canAfford;
            choiceButtons[i].GetComponent<Image>().color = canAfford ? Brown : new Color(Brown.r, Brown.g, Brown.b, 0.4f);
        }

        root.SetActive(true);
    }

    private string Label(EventChoice c, bool canAfford)
    {
        string cost = "";
        if (c.goldCost > 0) cost += $"  (골드 -{c.goldCost})";
        if (c.hpCost > 0) cost += $"  (체력 -{c.hpCost})";
        if (!canAfford) cost += "  - 부족";
        return c.text + cost;
    }

    // ---------------- 선택 ----------------

    private void Choose(int index)
    {
        root.SetActive(false);
        EventChoice c = current.choices[index];
        RunState run = RunState.GetOrCreate();
        List<string> lines = new List<string>();
        List<CardData> cards = new List<CardData>();

        if (!string.IsNullOrEmpty(c.resultText)) lines.Add(c.resultText);

        // 대가 치르기 (이벤트로는 죽지 않게 체력은 최소 1)
        if (c.goldCost > 0) { run.gold -= c.goldCost; lines.Add($"골드 -{c.goldCost}"); }
        if (c.hpCost > 0) { run.playerHP = Mathf.Max(1, run.playerHP - c.hpCost); lines.Add($"체력 -{c.hpCost}"); }

        // 함정
        if (c.trapChance > 0f && Random.value < c.trapChance)
        {
            run.playerHP = Mathf.Max(1, run.playerHP - c.trapHpLoss);
            lines.Add($"함정이다! 체력 -{c.trapHpLoss}");
        }

        // 보상
        switch (c.reward)
        {
            case EventRewardType.Gold:
                int gold = Random.Range(c.goldMin, c.goldMax + 1);
                run.gold += gold;
                lines.Add($"골드 +{gold}");
                break;

            case EventRewardType.Relic:
                RelicData relic = c.relic != null ? c.relic : PickRelic(run);
                if (relic != null && run.AddRelic(relic))
                {
                    lines.Add($"유물 획득: {relic.relicName}");
                }
                else
                {
                    run.gold += 30;
                    lines.Add("더 얻을 유물이 없어 대신 골드 +30");
                }
                break;

            case EventRewardType.Card:
                cards = PickCards(3);
                break;

            case EventRewardType.Item:
                lines.Add("1회용 아이템 (준비 중)");
                break;
        }

        lines.Add($"체력 {run.playerHP} / {run.playerMaxHP}   ·   골드 {run.gold}");
        Debug.Log($"[이벤트] {current.title} → {c.text}");

        // 결과는 전투 보상 창과 같은 모양으로 보여준다 (카드 보상이면 3장 중 고르기)
        BattleRewardPopup.Show(current.title, lines, cards, picked =>
        {
            if (picked != null)
            {
                run.obtainedCards.Add(picked);
                Debug.Log($"이벤트 카드 보상: {picked.cardName} 획득!");
            }
        });
    }

    private RelicData PickRelic(RunState run)
    {
        if (relicPool == null) return null;
        List<RelicData> pool = new List<RelicData>();
        foreach (RelicData r in relicPool)
            if (r != null && !run.ownedRelics.Contains(r)) pool.Add(r);
        return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
    }

    private List<CardData> PickCards(int count)
    {
        List<CardData> result = new List<CardData>();
        if (cardPool == null) return result;
        List<CardData> pool = new List<CardData>(cardPool);
        while (result.Count < count && pool.Count > 0)
        {
            int r = Random.Range(0, pool.Count);
            if (pool[r] != null) result.Add(pool[r]);
            pool.RemoveAt(r);
        }
        return result;
    }

    // ---------------- 화면 만들기 ----------------

    private void BuildUI()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        root = new GameObject("EventCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Image dim = NewImage("Dim", root.transform, new Color(0, 0, 0, 0.67f));
        dim.rectTransform.anchorMin = Vector2.zero;
        dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.offsetMin = Vector2.zero;
        dim.rectTransform.offsetMax = Vector2.zero;

        Image window = NewImage("Window", dim.transform, Parchment);
        window.rectTransform.sizeDelta = new Vector2(960, 800);
        Outline border = window.gameObject.AddComponent<Outline>();
        border.effectColor = Ink;
        border.effectDistance = new Vector2(4, -4);

        picture = NewImage("Picture", window.transform, Color.white);
        picture.rectTransform.sizeDelta = new Vector2(880, 260);
        picture.rectTransform.anchoredPosition = new Vector2(0, 240);
        picture.preserveAspect = true;
        picture.raycastTarget = false;

        titleText = NewText("Title", window.transform, "", 44, FontStyle.Bold, Ink, new Vector2(0, 70), new Vector2(880, 60));
        storyText = NewText("Story", window.transform, "", 24, FontStyle.Normal, InkLight, new Vector2(0, -40), new Vector2(860, 140));
        storyText.horizontalOverflow = HorizontalWrapMode.Wrap;

        for (int i = 0; i < 3; i++)
        {
            int index = i;
            Image b = NewImage("Choice" + i, window.transform, Brown);
            b.rectTransform.sizeDelta = new Vector2(840, 62);
            b.rectTransform.anchoredPosition = new Vector2(0, -170 - i * 76);
            choiceTexts[i] = NewText("Label", b.transform, "", 24, FontStyle.Bold, Color.white, Vector2.zero, new Vector2(820, 62));
            choiceButtons[i] = b.gameObject.AddComponent<Button>();
            choiceButtons[i].onClick.AddListener(() => Choose(index));
        }
    }

    private Image NewImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private Text NewText(string name, Transform parent, string content, int size, FontStyle style, Color color, Vector2 pos, Vector2 box)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = box;
        text.rectTransform.anchoredPosition = pos;
        return text;
    }
}