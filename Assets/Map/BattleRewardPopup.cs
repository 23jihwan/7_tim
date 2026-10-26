using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 전투 씬 위에 보상 창을 코드로 만들어 띄운다 (전투 씬을 따로 수정할 필요 없음)
// 카드를 하나 골라 두고(선택 사항) 확인을 누르면 onConfirm이 호출된다
public static class BattleRewardPopup
{
    private static readonly Color Parchment = new Color32(0xEA, 0xDF, 0xC3, 0xFF);
    private static readonly Color Ink = new Color32(0x3A, 0x2C, 0x1A, 0xFF);
    private static readonly Color InkLight = new Color32(0x5A, 0x46, 0x30, 0xFF);
    private static readonly Color Gold = new Color32(0xF2, 0xB4, 0x5A, 0xFF);
    private static readonly Color Red = new Color32(0x8F, 0x2A, 0x1E, 0xFF);

    // 전투에서 쓰는 카드 프리팹 (테두리, 이름, 설명이 있는 진짜 카드 모양)
    // RunState와 EventPopup이 넣어 준다. 없으면 예전처럼 그림만 보여준다
    public static GameObject cardPrefab;

    public static void Show(string title, List<string> lines, List<CardData> cardChoices, Action<CardData> onConfirm)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 1. 맨 위에 그려지는 Canvas
        GameObject canvasGO = new GameObject("RewardCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // 2. 어두운 배경 (뒤의 전투 화면을 누를 수 없게 막는다)
        Image dim = CreateImage("Dim", canvasGO.transform, new Color(0, 0, 0, 0.67f));
        RectTransform dimRt = dim.rectTransform;
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;

        // 3. 창
        bool hasCards = cardChoices != null && cardChoices.Count > 0;
        float height = hasCards ? 680 : 420;
        Image window = CreateImage("Window", dim.transform, Parchment);
        window.rectTransform.sizeDelta = new Vector2(900, height);
        Outline border = window.gameObject.AddComponent<Outline>();
        border.effectColor = Ink;
        border.effectDistance = new Vector2(4, -4);

        float top = height / 2f;
        CreateText("Title", window.transform, font, title, 48, FontStyle.Bold, Ink, new Vector2(0, top - 70), new Vector2(800, 70));

        float y = top - 140;
        foreach (string line in lines)
        {
            CreateText("Line", window.transform, font, line, 28, FontStyle.Normal, InkLight, new Vector2(0, y), new Vector2(800, 44));
            y -= 44;
        }

        // 4. 카드 고르기 (선택 사항)
        CardData selected = null;
        List<Outline> highlights = new List<Outline>();

        if (hasCards)
        {
            CreateText("Guide", window.transform, font, "카드를 1장 고르세요 (고르지 않으면 건너뜀)", 22, FontStyle.Normal, InkLight, new Vector2(0, y - 10), new Vector2(800, 36));

            float cardY = y - 150;
            for (int i = 0; i < cardChoices.Count; i++)
            {
                CardData card = cardChoices[i];
                float x = (i - (cardChoices.Count - 1) / 2f) * 200f;

                Image cardImage;
                if (cardPrefab != null)
                {
                    // 진짜 카드 모양: 금색 선택 테두리 + 카드 프리팹
                    cardImage = CreateImage("Card", window.transform, Gold);
                    cardImage.rectTransform.sizeDelta = new Vector2(162, 216);
                    cardImage.rectTransform.anchoredPosition = new Vector2(x, cardY + 10);

                    GameObject cardObj = UnityEngine.Object.Instantiate(cardPrefab, cardImage.transform);
                    RectTransform crt = (RectTransform)cardObj.transform;
                    crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
                    crt.pivot = new Vector2(0.5f, 0.5f);
                    crt.anchoredPosition = Vector2.zero;
                    crt.sizeDelta = new Vector2(150, 204);
                    crt.localScale = Vector3.one;

                    CardUI ui = cardObj.GetComponent<CardUI>();
                    if (ui != null)
                    {
                        ui.SetupCard(card);
                        ui.enabled = false;   // 보상 창에서는 끌기, 확대 미리보기를 끈다
                    }
                    CanvasGroup group = cardObj.GetComponent<CanvasGroup>();
                    if (group == null) group = cardObj.AddComponent<CanvasGroup>();
                    group.blocksRaycasts = false;   // 클릭은 바깥의 금색 테두리 버튼이 받는다
                    LayoutElement layout = cardObj.GetComponent<LayoutElement>();
                    if (layout != null) layout.ignoreLayout = true;

                    cardImage.color = new Color(Gold.r, Gold.g, Gold.b, 0f);   // 선택 전에는 테두리 숨김
                }
                else
                {
                    cardImage = CreateImage("Card", window.transform, Color.white);
                    cardImage.rectTransform.sizeDelta = new Vector2(140, 190);
                    cardImage.rectTransform.anchoredPosition = new Vector2(x, cardY);
                    if (card.cardIcon != null) cardImage.sprite = card.cardIcon;
                    CreateText("Name", cardImage.transform, font, card.cardName, 24, FontStyle.Bold, Ink, new Vector2(0, -120), new Vector2(200, 36));
                }

                Outline glow = cardImage.gameObject.AddComponent<Outline>();
                glow.effectColor = Gold;
                glow.effectDistance = new Vector2(6, -6);
                glow.enabled = false;
                highlights.Add(glow);
                Button cardButton = cardImage.gameObject.AddComponent<Button>();
                int index = i;
                cardButton.onClick.AddListener(() =>
                {
                    // 같은 카드를 다시 누르면 선택 취소
                    selected = (selected == card) ? null : card;
                    SoundManager.Play(selected != null ? "card_place" : "click");
                    for (int k = 0; k < highlights.Count; k++)
                    {
                        bool on = selected != null && k == index;
                        highlights[k].enabled = on;
                        highlights[k].transform.localScale = Vector3.one * (on ? 1.08f : 1f);
                        if (cardPrefab != null)
                        {
                            Image frame = highlights[k].GetComponent<Image>();
                            frame.color = new Color(Gold.r, Gold.g, Gold.b, on ? 1f : 0f);
                        }
                    }
                });
            }
        }

        // 5. 확인 버튼
        Image confirmImage = CreateImage("ConfirmButton", window.transform, Red);
        confirmImage.rectTransform.sizeDelta = new Vector2(240, 66);
        confirmImage.rectTransform.anchoredPosition = new Vector2(0, -top + 60);
        CreateText("Label", confirmImage.transform, font, "확인", 30, FontStyle.Bold, Color.white, Vector2.zero, new Vector2(240, 66));
        Button confirmButton = confirmImage.gameObject.AddComponent<Button>();
        confirmButton.onClick.AddListener(() =>
        {
            SoundManager.Play("click");
            UnityEngine.Object.Destroy(canvasGO);
            onConfirm?.Invoke(selected);
        });
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(string name, Transform parent, Font font, string content, int size, FontStyle style, Color color, Vector2 pos, Vector2 boxSize)
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
        RectTransform rt = text.rectTransform;
        rt.sizeDelta = boxSize;
        rt.anchoredPosition = pos;
        return text;
    }
}
