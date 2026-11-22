using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 조합법 보상 고르기 창 (엘리트 · 보스 처치, 이벤트)
// 후보 조합법 몇 개를 보여주고, 하나를 골라 "배우기"를 누르면 그 조합법을 얻는다
// RecipeRewardPopup.Show(제목, 후보 목록, 끝나면 할 일) 한 줄로 쓴다
public static class RecipeRewardPopup
{
    private static readonly Color Parchment = new Color32(0xEA, 0xDF, 0xC3, 0xFF);
    private static readonly Color Row = new Color32(0xDD, 0xD0, 0xB0, 0xFF);
    private static readonly Color Ink = new Color32(0x3A, 0x2C, 0x1A, 0xFF);
    private static readonly Color InkLight = new Color32(0x5A, 0x46, 0x30, 0xFF);
    private static readonly Color Brown = new Color32(0x7A, 0x64, 0x48, 0xFF);
    private static readonly Color Gold = new Color32(0xE2, 0xB5, 0x52, 0xFF);
    private static readonly Color Purple = new Color32(0x5B, 0x3A, 0x7A, 0xFF);

    /// <param name="onDone">배웠으면 true, 건너뛰었으면 false</param>
    public static void Show(string title, List<CardRecipe> options, Action<bool> onDone)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasGO = new GameObject("RecipeRewardPopup", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 170;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Image dim = NewImage("Dim", canvasGO.transform, new Color(0, 0, 0, 0.72f));
        dim.rectTransform.anchorMin = Vector2.zero; dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.offsetMin = Vector2.zero; dim.rectTransform.offsetMax = Vector2.zero;

        float rowH = 130f, gap = 18f;
        float listH = options.Count * rowH + (options.Count - 1) * gap;
        Image window = NewImage("Window", dim.transform, Parchment);
        window.rectTransform.sizeDelta = new Vector2(1000, listH + 300);
        Outline o = window.gameObject.AddComponent<Outline>(); o.effectColor = Ink; o.effectDistance = new Vector2(4, -4);
        float top = (listH + 300) / 2f;

        NewText(font, "Title", window.transform, title, 40, FontStyle.Bold, Ink, new Vector2(0, top - 55), new Vector2(940, 56));
        NewText(font, "Sub", window.transform, "배울 조합법을 하나 고르세요", 22, FontStyle.Normal, InkLight, new Vector2(0, top - 100), new Vector2(940, 30));

        int selected = -1;
        List<Image> rows = new List<Image>();
        Button learn = NewButton(font, "Learn", window.transform, "배우기", Purple, new Vector2(150, -top + 60), new Vector2(260, 62));
        Button skip = NewButton(font, "Skip", window.transform, "건너뛰기", Brown, new Vector2(-150, -top + 60), new Vector2(260, 62));
        SetInteractable(learn, false, Purple);

        for (int i = 0; i < options.Count; i++)
        {
            int index = i;
            CardRecipe r = options[i];
            float y = top - 150 - rowH / 2f - i * (rowH + gap);

            Image row = NewImage("Row" + i, window.transform, Row);
            row.rectTransform.sizeDelta = new Vector2(900, rowH);
            row.rectTransform.anchoredPosition = new Vector2(0, y);
            Outline ro = row.gameObject.AddComponent<Outline>(); ro.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0f); ro.effectDistance = new Vector2(5, -5);
            rows.Add(row);

            Icon(row.transform, r.cardA, -360);
            NewText(font, "Plus", row.transform, "+", 34, FontStyle.Bold, Ink, new Vector2(-290, 0), new Vector2(40, 40));
            Icon(row.transform, r.cardB, -220);
            NewText(font, "Arrow", row.transform, "→", 34, FontStyle.Bold, Ink, new Vector2(-140, 0), new Vector2(50, 40));
            Icon(row.transform, r.resultCard, -60);

            string a = r.cardA != null ? r.cardA.cardName : "?";
            string b = r.cardB != null ? r.cardB.cardName : "?";
            Text name = NewText(font, "Result", row.transform, r.resultCard != null ? r.resultCard.cardName : "?", 30, FontStyle.Bold, Ink, new Vector2(200, 18), new Vector2(380, 40));
            name.alignment = TextAnchor.MiddleLeft;
            Text recipe = NewText(font, "Recipe", row.transform, $"{a} + {b}", 20, FontStyle.Normal, InkLight, new Vector2(200, -22), new Vector2(380, 30));
            recipe.alignment = TextAnchor.MiddleLeft;

            Button btn = row.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() =>
            {
                selected = index;
                for (int k = 0; k < rows.Count; k++)
                {
                    rows[k].color = k == index ? new Color32(0xF2, 0xE4, 0xBE, 0xFF) : Row;
                    rows[k].GetComponent<Outline>().effectColor = new Color(Gold.r, Gold.g, Gold.b, k == index ? 1f : 0f);
                }
                SetInteractable(learn, true, Purple);
                SoundManager.Play("card_place");
            });
        }

        learn.onClick.AddListener(() =>
        {
            if (selected < 0) return;
            RecipeUnlock.Unlock(options[selected]);
            SoundManager.Play("combine");
            UnityEngine.Object.Destroy(canvasGO);
            onDone?.Invoke(true);
        });
        skip.onClick.AddListener(() =>
        {
            SoundManager.Play("click");
            UnityEngine.Object.Destroy(canvasGO);
            onDone?.Invoke(false);
        });
        SoundManager.Play("page");
    }

    private static void Icon(Transform parent, CardData card, float x)
    {
        Image frame = NewImage("IconFrame", parent, Ink);
        frame.rectTransform.sizeDelta = new Vector2(96, 96);
        frame.rectTransform.anchoredPosition = new Vector2(x, 0);
        Image icon = NewImage("Icon", frame.transform, Color.white);
        icon.rectTransform.sizeDelta = new Vector2(88, 88);
        icon.preserveAspect = true; icon.raycastTarget = false;
        if (card != null && card.cardIcon != null) icon.sprite = card.cardIcon;
        frame.raycastTarget = false;
    }

    private static void SetInteractable(Button b, bool on, Color color)
    {
        b.interactable = on;
        b.GetComponent<Image>().color = on ? color : new Color(color.r, color.g, color.b, 0.35f);
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
