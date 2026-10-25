using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 전투 화면 구석에 덱 더미를 보여주고, 마우스를 올리면 조합법 목록을 띄운다
// 아직 모르는 조합은 결과가 "?"로, 알아낸 조합은 결과 카드 그림이 나온다
// 전투 씬의 빈 오브젝트에 붙이고 Recipe Database만 연결하면 된다 (화면은 코드가 만든다)
public class DeckRecipeView : MonoBehaviour
{
    [Header("데이터")]
    public CardRecipeDatabase recipeDatabase;

    [Header("덱 더미 위치 (화면 오른쪽 아래 기준)")]
    public Vector2 deckPosition = new Vector2(-110, 40);
    [Tooltip("카드 뒷면 그림 (비워 두면 기본 모양)")]
    public Sprite cardBack;

    private static readonly Color Parchment = new Color32(0xEA, 0xDF, 0xC3, 0xFF);
    private static readonly Color Ink = new Color32(0x3A, 0x2C, 0x1A, 0xFF);
    private static readonly Color InkLight = new Color32(0x5A, 0x46, 0x30, 0xFF);
    private static readonly Color Purple = new Color32(0x3B, 0x2A, 0x55, 0xFF);
    private static readonly Color Gold = new Color32(0xC9, 0xA1, 0x4A, 0xFF);

    private Font font;
    private Text countText;
    private GameObject panel;
    private Text headerText;
    private RectTransform listRoot;
    private int lastCount = -1;

    private void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUI();
        panel.SetActive(false);
    }

    private void Update()
    {
        int count = DeckCount();
        if (count != lastCount)
        {
            lastCount = count;
            countText.text = count.ToString();
        }
    }

    private int DeckCount()
    {
        if (RunState.Instance != null && RunState.Instance.deck.Count > 0) return RunState.Instance.deck.Count;
        GameManager gm = GameManager.Instance;
        return gm != null && gm.cardDatabase != null ? gm.cardDatabase.Count : 0;
    }

    // ---------------- 마우스 ----------------

    private void ShowPanel()
    {
        RefreshList();
        panel.SetActive(true);
    }

    private void HidePanel()
    {
        panel.SetActive(false);
    }

    [ContextMenu("테스트: 조합법 해금 기록 지우기")]
    private void ResetRecipes() { RecipeBook.ResetAll(); }

    [ContextMenu("테스트: 모든 조합법 해금")]
    private void UnlockAll()
    {
        if (recipeDatabase == null) return;
        foreach (CardRecipe r in recipeDatabase.recipes) RecipeBook.Unlock(r.cardA, r.cardB);
    }

    // ---------------- 조합법 목록 ----------------

    private void RefreshList()
    {
        foreach (Transform child in listRoot) Destroy(child.gameObject);
        if (recipeDatabase == null) { headerText.text = "조합법 (데이터 없음)"; return; }

        int known = 0;
        List<CardRecipe> recipes = recipeDatabase.recipes;
        for (int i = 0; i < recipes.Count; i++)
        {
            CardRecipe r = recipes[i];
            bool unlocked = RecipeBook.IsUnlocked(r.cardA, r.cardB);
            if (unlocked) known++;
            BuildRow(r, unlocked, -i * 58f);
        }
        headerText.text = $"조합법   {known} / {recipes.Count}";
    }

    private void BuildRow(CardRecipe r, bool unlocked, float y)
    {
        Image row = NewImage("Row", listRoot, new Color(1f, 1f, 1f, unlocked ? 0.35f : 0.15f));
        RectTransform rt = row.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(580, 52);
        rt.anchoredPosition = new Vector2(0, y);

        Icon(row.transform, r.cardA, -250);
        NewText("Plus", row.transform, "+", 26, FontStyle.Bold, InkLight, TextAnchor.MiddleCenter, new Vector2(-205, 0), new Vector2(30, 40));
        Icon(row.transform, r.cardB, -160);
        NewText("Arrow", row.transform, "→", 26, FontStyle.Bold, InkLight, TextAnchor.MiddleCenter, new Vector2(-112, 0), new Vector2(40, 40));

        if (unlocked && r.resultCard != null)
        {
            Icon(row.transform, r.resultCard, -62);
            NewText("Name", row.transform, r.resultCard.cardName, 22, FontStyle.Bold, Ink, TextAnchor.MiddleLeft, new Vector2(110, 0), new Vector2(280, 40));
        }
        else
        {
            Image q = NewImage("Unknown", row.transform, Purple);
            q.rectTransform.sizeDelta = new Vector2(40, 44);
            q.rectTransform.anchoredPosition = new Vector2(-62, 0);
            NewText("Q", q.transform, "?", 28, FontStyle.Bold, Gold, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(40, 44));
            NewText("Name", row.transform, "???", 22, FontStyle.Bold, InkLight, TextAnchor.MiddleLeft, new Vector2(110, 0), new Vector2(280, 40));
        }
    }

    private void Icon(Transform parent, CardData card, float x)
    {
        Image img = NewImage("Icon", parent, Color.white);
        img.rectTransform.sizeDelta = new Vector2(40, 44);
        img.rectTransform.anchoredPosition = new Vector2(x, 0);
        img.preserveAspect = true;
        if (card != null && card.cardIcon != null) img.sprite = card.cardIcon;
        else img.color = Purple;
    }

    // ---------------- 화면 만들기 ----------------

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("DeckRecipeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // 덱 더미 (카드 3장이 겹친 모양)
        Image hit = NewImage("Deck", canvasGO.transform, new Color(0, 0, 0, 0));
        RectTransform hrt = hit.rectTransform;
        hrt.anchorMin = hrt.anchorMax = new Vector2(1f, 0f);
        hrt.pivot = new Vector2(0.5f, 0f);
        hrt.sizeDelta = new Vector2(130, 175);
        hrt.anchoredPosition = deckPosition;

        for (int i = 2; i >= 0; i--)
        {
            Image back = NewImage("Back" + i, hit.transform, cardBack != null ? Color.white : Purple);
            back.rectTransform.sizeDelta = new Vector2(110, 150);
            back.rectTransform.anchoredPosition = new Vector2(i * 5f, i * 5f);
            back.raycastTarget = false;
            if (cardBack != null) back.sprite = cardBack;
            else
            {
                Outline o = back.gameObject.AddComponent<Outline>();
                o.effectColor = Gold;
                o.effectDistance = new Vector2(3, -3);
                if (i == 0)
                {
                    Image gem = NewImage("Gem", back.transform, Gold);
                    gem.rectTransform.sizeDelta = new Vector2(36, 36);
                    gem.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                    gem.raycastTarget = false;
                }
            }
        }

        Image badge = NewImage("Count", hit.transform, Ink);
        badge.rectTransform.sizeDelta = new Vector2(52, 40);
        badge.rectTransform.anchoredPosition = new Vector2(0, -70);
        badge.raycastTarget = false;
        Outline bo = badge.gameObject.AddComponent<Outline>();
        bo.effectColor = Gold;
        bo.effectDistance = new Vector2(2, -2);
        countText = NewText("Text", badge.transform, "0", 24, FontStyle.Bold, Parchment, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(52, 40));
        NewText("Label", hit.transform, "덱", 20, FontStyle.Bold, Parchment, TextAnchor.MiddleCenter, new Vector2(0, 92), new Vector2(80, 30));

        // 마우스를 올리면 조합법 창 (EventTrigger로 감지)
        EventTrigger trigger = hit.gameObject.AddComponent<EventTrigger>();
        AddTrigger(trigger, EventTriggerType.PointerEnter, ShowPanel);
        AddTrigger(trigger, EventTriggerType.PointerExit, HidePanel);

        // 조합법 창 (덱 왼쪽 위에 뜬다)
        Image p = NewImage("RecipePanel", canvasGO.transform, Parchment);
        RectTransform prt = p.rectTransform;
        prt.anchorMin = prt.anchorMax = new Vector2(1f, 0f);
        prt.pivot = new Vector2(1f, 0f);
        float h = 90 + (recipeDatabase != null ? recipeDatabase.recipes.Count : 6) * 58f;
        prt.sizeDelta = new Vector2(620, h);
        prt.anchoredPosition = new Vector2(deckPosition.x - 80, deckPosition.y + 20);
        p.raycastTarget = false;
        Outline po = p.gameObject.AddComponent<Outline>();
        po.effectColor = Ink;
        po.effectDistance = new Vector2(4, -4);
        panel = p.gameObject;

        headerText = NewText("Header", p.transform, "조합법", 28, FontStyle.Bold, Ink, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(580, 50));
        RectTransform hdr = headerText.rectTransform;
        hdr.anchorMin = hdr.anchorMax = new Vector2(0.5f, 1f);
        hdr.anchoredPosition = new Vector2(0, -36);

        GameObject list = new GameObject("List", typeof(RectTransform));
        list.transform.SetParent(p.transform, false);
        listRoot = (RectTransform)list.transform;
        listRoot.anchorMin = listRoot.anchorMax = new Vector2(0.5f, 1f);
        listRoot.pivot = new Vector2(0.5f, 1f);
        listRoot.sizeDelta = new Vector2(580, h - 80);
        listRoot.anchoredPosition = new Vector2(0, -72);
    }

    private void AddTrigger(EventTrigger trigger, EventTriggerType type, System.Action action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }

    private Image NewImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private Text NewText(string name, Transform parent, string content, int size, FontStyle style, Color color, TextAnchor align, Vector2 pos, Vector2 box)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = align;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = box;
        text.rectTransform.anchoredPosition = pos;
        return text;
    }
}