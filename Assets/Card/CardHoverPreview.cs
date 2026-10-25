using UnityEngine;

// 손패의 카드에 마우스를 올리면, 그 카드의 크게 확대된 복사본을 카드 위쪽에 보여준다
// 원래 카드는 그대로 두기 때문에 손패 정렬이나 드래그에 영향을 주지 않는다
public static class CardHoverPreview
{
    public static float scale = 1.8f;    // 확대 배율
    public static float gap = 12f;       // 원래 카드와 복사본 사이 간격

    private static GameObject preview;
    private static CardUI source;

    public static void Show(CardUI card)
    {
        Hide();
        if (card == null) return;

        Canvas canvas = card.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        Transform root = canvas.rootCanvas.transform;

        RectTransform src = (RectTransform)card.transform;

        // 1. 카드 복사본 만들기 (맨 위 Canvas에 붙여서 다른 UI보다 위에 그려지게)
        preview = Object.Instantiate(card.gameObject, root);
        preview.name = "CardPreview";
        source = card;

        // 2. 복사본은 보기만 하는 용도: 드래그·마우스 반응 끄기
        CardUI previewUI = preview.GetComponent<CardUI>();
        if (previewUI != null) previewUI.enabled = false;
        CanvasGroup group = preview.GetComponent<CanvasGroup>();
        if (group == null) group = preview.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.alpha = 1f;
        UnityEngine.UI.LayoutElement layout = preview.GetComponent<UnityEngine.UI.LayoutElement>();
        if (layout != null) layout.ignoreLayout = true;

        // 3. 원래 카드의 위쪽 가장자리에서 위로 커지게 배치
        RectTransform rt = (RectTransform)preview.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = src.rect.size;
        rt.pivot = new Vector2(0.5f, 0f);
        rt.localScale = Vector3.one * scale;
        rt.rotation = Quaternion.identity;

        Vector3 topCenter = src.TransformPoint(new Vector3(src.rect.center.x, src.rect.yMax, 0f));
        rt.position = topCenter + root.up * gap * root.lossyScale.y;
        rt.SetAsLastSibling();
    }

    // 지금 보여주는 미리보기가 이 카드의 것일 때만 숨긴다
    public static void Hide(CardUI card)
    {
        if (source == card) Hide();
    }

    public static void Hide()
    {
        if (preview != null) Object.Destroy(preview);
        preview = null;
        source = null;
    }
}