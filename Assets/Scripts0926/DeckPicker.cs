using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 내 덱의 카드를 모두 보여주고 1장을 고르게 하는 창 (휴식 방의 강화, 명상에서 쓴다)
// DeckPicker.Show(...) 한 줄로 쓴다. 화면은 코드가 만든다
public static class DeckPicker
{
    private static readonly Color Parchment = new Color32(0xEA, 0xDF, 0xC3, 0xFF);
    private static readonly Color Ink = new Color32(0x3A, 0x2C, 0x1A, 0xFF);
    private static readonly Color InkLight = new Color32(0x5A, 0x46, 0x30, 0xFF);
    private static readonly Color Brown = new Color32(0x7A, 0x64, 0x48, 0xFF);
    private static readonly Color Gold = new Color32(0xE2, 0xB5, 0x52, 0xFF);

    /// <param name="canPick">고를 수 있는 카드인지 (예: +5 카드는 강화 불가)</param>
    /// <param name="describe">고른 카드에 대해 아래에 보여줄 설명 (예: "파이어볼+1 → 파이어볼+2")</param>
    /// <param name="onPicked">확인을 누르면 덱에서의 번호를 알려준다</param>
    /// <param name="onCancel">돌아가기를 누르면</param>
    // 덱 보기 전용 (고르기 없이 구경만, 카드를 누르면 설명이 나온다)
    public static void ShowView()
    {
        Show("내 덱", "", Color.gray, null,
             card => $"{card.cardName}  -  {Describe(card)}",
             null, null, true);
    }

    private static string Describe(CardData card)
    {
        if (!string.IsNullOrEmpty(card.description)) return card.description.Replace("\n", " ");
        switch (card.cardType)
        {
            case CardType.Attack: return $"적에게 {card.value} 피해";
            case CardType.Heal: return card.damageValue > 0 ? $"체력 {card.value} 회복, 적에게 {card.damageValue} 피해" : $"체력 {card.value} 회복";
            case CardType.Debuff: return $"이번 턴 적 공격력 -{card.value}";
        }
        return $"{card.cardType} {card.value}";
    }

    public static void Show(string title, string confirmLabel, Color confirmColor,
        Func<CardData, bool> canPick, Func<CardData, string> describe,
        Action<int> onPicked, Action onCancel, bool viewOnly = false)
    {
        RunState run = RunState.GetOrCreate();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasGO = new GameObject("DeckPicker", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 160;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Image dim = NewImage("Dim", canvasGO.transform, new Color(0, 0, 0, 0.75f));
        Stretch(dim.rectTransform);

        Image window = NewImage("Window", dim.transform, Parchment);
        window.rectTransform.sizeDelta = new Vector2(1160, 900);
        Outline o = window.gameObject.AddComponent<Outline>(); o.effectColor = Ink; o.effectDistance = new Vector2(4, -4);

        NewText(font, "Title", window.transform, title, 40, FontStyle.Bold, Ink, new Vector2(0, 400), new Vector2(1100, 60));
        NewText(font, "Count", window.transform, $"내 덱 {run.deck.Count}장", 22, FontStyle.Normal, InkLight, new Vector2(0, 358), new Vector2(1100, 30));

        // 스크롤 영역
        Image view = NewImage("Viewport", window.transform, new Color(0, 0, 0, 0.08f));
        view.rectTransform.sizeDelta = new Vector2(1080, 600);
        view.rectTransform.anchoredPosition = new Vector2(0, 30);
        view.gameObject.AddComponent<RectMask2D>();

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(view.transform, false);
        RectTransform crt = (RectTransform)content.transform;
        crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1); crt.pivot = new Vector2(0.5f, 1);
        crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(150, 210);
        grid.spacing = new Vector2(24, 24);
        grid.padding = new RectOffset(24, 24, 20, 20);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 6;
        grid.childAlignment = TextAnchor.UpperCenter;
        ContentSizeFitter fit = content.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.content = crt; scroll.viewport = view.rectTransform;
        scroll.horizontal = false; scroll.scrollSensitivity = 30f;

        // 아래쪽: 선택 설명 + 버튼
        Text info = NewText(font, "Info", window.transform, viewOnly ? "카드를 누르면 효과를 볼 수 있어요" : "카드를 골라 주세요", 26, FontStyle.Bold, InkLight, new Vector2(0, -300), new Vector2(1100, 40));

        int selected = -1;
        List<Image> frames = new List<Image>();

        Button confirm = NewButton(font, "Confirm", window.transform, confirmLabel, confirmColor, new Vector2(160, -380), new Vector2(280, 64));
        Button cancel = NewButton(font, "Cancel", window.transform, viewOnly ? "닫기" : "돌아가기", Brown, new Vector2(viewOnly ? 0 : -160, -380), new Vector2(280, 64));
        SetInteractable(confirm, false, confirmColor);
        if (viewOnly) confirm.gameObject.SetActive(false);

        for (int i = 0; i < run.deck.Count; i++)
        {
            int index = i;
            CardData card = run.deck[i];
            bool ok = canPick == null || canPick(card);

            Image frame = NewImage("Card", content.transform, new Color(Gold.r, Gold.g, Gold.b, 0f));
            frames.Add(frame);

            GameObject prefab = BattleRewardPopup.cardPrefab;
            if (prefab != null)
            {
                GameObject obj = UnityEngine.Object.Instantiate(prefab, frame.transform);
                RectTransform rt = (RectTransform)obj.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(140, 196); rt.localScale = Vector3.one;
                CardUI ui = obj.GetComponent<CardUI>();
                if (ui != null) { ui.SetupCard(card); ui.enabled = false; }
                CanvasGroup g = obj.GetComponent<CanvasGroup>(); if (g == null) g = obj.AddComponent<CanvasGroup>();
                g.blocksRaycasts = false; g.alpha = ok ? 1f : 0.35f;
                LayoutElement le = obj.GetComponent<LayoutElement>(); if (le != null) le.ignoreLayout = true;
            }
            else
            {
                Image icon = NewImage("Icon", frame.transform, Color.white);
                icon.rectTransform.sizeDelta = new Vector2(130, 130); icon.preserveAspect = true;
                if (card.cardIcon != null) icon.sprite = card.cardIcon;
                NewText(font, "Name", frame.transform, card.cardName, 20, FontStyle.Bold, Ink, new Vector2(0, -86), new Vector2(150, 30));
                if (!ok) icon.color = new Color(1, 1, 1, 0.35f);
            }

            if (!ok) continue;
            Button b = frame.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() =>
            {
                selected = index;
                for (int k = 0; k < frames.Count; k++)
                    frames[k].color = new Color(Gold.r, Gold.g, Gold.b, k == index ? 1f : 0f);
                info.text = describe != null ? describe(run.deck[index]) : run.deck[index].cardName;
                if (!viewOnly) SetInteractable(confirm, true, confirmColor);
                SoundManager.Play("card_place");
            });
        }

        confirm.onClick.AddListener(() =>
        {
            if (selected < 0) return;
            UnityEngine.Object.Destroy(canvasGO);
            onPicked?.Invoke(selected);
        });
        cancel.onClick.AddListener(() =>
        {
            SoundManager.Play("click");
            UnityEngine.Object.Destroy(canvasGO);
            onCancel?.Invoke();
        });
        SoundManager.Play("page");
    }

    private static void SetInteractable(Button b, bool on, Color color)
    {
        b.interactable = on;
        b.GetComponent<Image>().color = on ? color : new Color(color.r, color.g, color.b, 0.35f);
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>(); image.color = color; return image;
    }

    private static Text NewText(Font font, string name, Transform parent, string content, int size, FontStyle style, Color color, Vector2 pos, Vector2 box)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text t = go.GetComponent<Text>();
        t.font = font; t.text = content; t.fontSize = size; t.fontStyle = style; t.color = color;
        t.alignment = TextAnchor.MiddleCenter; t.horizontalOverflow = HorizontalWrapMode.Overflow; t.raycastTarget = false;
        t.rectTransform.sizeDelta = box; t.rectTransform.anchoredPosition = pos;
        return t;
    }

    private static Button NewButton(Font font, string name, Transform parent, string label, Color color, Vector2 pos, Vector2 size)
    {
        Image img = NewImage(name, parent, color);
        img.rectTransform.sizeDelta = size; img.rectTransform.anchoredPosition = pos;
        NewText(font, "Label", img.transform, label, 26, FontStyle.Bold, Color.white, Vector2.zero, size);
        return img.gameObject.AddComponent<Button>();
    }
}
