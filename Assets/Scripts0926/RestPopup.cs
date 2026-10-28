using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 휴식 방: 맵에서 휴식 노드에 들어가면 뜨는 창
// 휴식(체력 회복)과 강화(준비 중, 비활성) 선택지, 그리고 지나가기
// 맵 씬의 빈 오브젝트에 붙이면 화면은 코드가 만든다
public class RestPopup : MonoBehaviour
{
    [Header("휴식")]
    [Tooltip("최대 체력의 몇 %를 회복할지")]
    [Range(0f, 1f)] public float healPercent = 0.3f;
    public Sprite image;

    [Header("강화 (준비 중)")]
    public bool upgradeEnabled = false;

    private static readonly Color Parchment = new Color32(0xEA, 0xDF, 0xC3, 0xFF);
    private static readonly Color Ink = new Color32(0x3A, 0x2C, 0x1A, 0xFF);
    private static readonly Color InkLight = new Color32(0x5A, 0x46, 0x30, 0xFF);
    private static readonly Color Brown = new Color32(0x7A, 0x64, 0x48, 0xFF);
    private static readonly Color Green = new Color32(0x4F, 0x7A, 0x3A, 0xFF);

    private Font font;
    private GameObject root;
    private RectTransform windowRect;
    private Image picture;
    private Text titleText;
    private Text storyText;
    private Button restButton, upgradeButton, leaveButton;
    private Text restLabel, upgradeLabel;

    private void Awake()
    {
        BuildUI();
        root.SetActive(false);
    }

    // ---------------- 열기 ----------------

    public void Open()
    {
        RunState run = RunState.GetOrCreate();
        int heal = HealAmount(run);

        bool hasImage = image != null;
        picture.gameObject.SetActive(hasImage);
        if (hasImage) picture.sprite = image;
        Layout(hasImage);

        storyText.text = $"따뜻한 모닥불이 지친 몸을 감싼다.\n현재 체력 {run.playerHP} / {run.playerMaxHP}";

        // 휴식: 체력이 가득 차 있으면 누를 수 없게
        bool canRest = heal > 0;
        restLabel.text = canRest ? $"휴식 - 체력을 {heal} 회복한다" : "휴식 - 체력이 이미 가득 차 있다";
        SetButton(restButton, canRest, Green);

        // 강화: 아직 준비 중이라 비활성
        upgradeLabel.text = upgradeEnabled ? "강화 - 카드 1장을 강화한다" : "강화 - 카드 1장을 강화한다  (준비 중)";
        SetButton(upgradeButton, upgradeEnabled, Brown);

        root.SetActive(true);
        SoundManager.Play("page");
    }

    private int HealAmount(RunState run)
    {
        int amount = Mathf.CeilToInt(run.playerMaxHP * healPercent);
        return Mathf.Min(amount, run.playerMaxHP - run.playerHP);
    }

    private void SetButton(Button b, bool on, Color color)
    {
        b.interactable = on;
        b.GetComponent<Image>().color = on ? color : new Color(color.r, color.g, color.b, 0.35f);
    }

    // ---------------- 선택 ----------------

    private void OnRest()
    {
        RunState run = RunState.GetOrCreate();
        int before = run.playerHP;
        run.playerHP = Mathf.Min(run.playerMaxHP, run.playerHP + HealAmount(run));
        root.SetActive(false);
        SoundManager.Play("heal");
        Debug.Log($"[휴식] 체력 회복: {before} → {run.playerHP}");

        List<string> lines = new List<string>
        {
            "모닥불 곁에서 잠시 눈을 붙였다.",
            $"체력 +{run.playerHP - before}",
            $"체력 {run.playerHP} / {run.playerMaxHP}"
        };
        BattleRewardPopup.Show("휴식", lines, new List<CardData>(), _ => { });
    }

    private void OnUpgrade()
    {
        // 카드 강화 기능이 생기면 여기에 연결한다
        Debug.Log("[휴식] 카드 강화는 아직 준비 중이에요.");
    }

    private void OnLeave()
    {
        root.SetActive(false);
        SoundManager.Play("click");
        Debug.Log("[휴식] 쉬지 않고 지나갔어요.");
    }

    // ---------------- 화면 만들기 ----------------

    private void Layout(bool hasImage)
    {
        float shift = hasImage ? 0f : 120f;
        windowRect.sizeDelta = new Vector2(960, hasImage ? 800 : 540);
        titleText.rectTransform.anchoredPosition = new Vector2(0, 70 + shift);
        storyText.rectTransform.anchoredPosition = new Vector2(0, -20 + shift);
        ((RectTransform)restButton.transform).anchoredPosition = new Vector2(0, -130 + shift);
        ((RectTransform)upgradeButton.transform).anchoredPosition = new Vector2(0, -206 + shift);
        ((RectTransform)leaveButton.transform).anchoredPosition = new Vector2(0, -300 + shift);
    }

    private void BuildUI()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        root = new GameObject("RestCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Image dim = NewImage("Dim", root.transform, new Color(0, 0, 0, 0.67f));
        dim.rectTransform.anchorMin = Vector2.zero; dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.offsetMin = Vector2.zero; dim.rectTransform.offsetMax = Vector2.zero;

        Image window = NewImage("Window", dim.transform, Parchment);
        windowRect = window.rectTransform;
        windowRect.sizeDelta = new Vector2(960, 800);
        Outline border = window.gameObject.AddComponent<Outline>();
        border.effectColor = Ink; border.effectDistance = new Vector2(4, -4);

        picture = NewImage("Picture", window.transform, Color.white);
        picture.rectTransform.sizeDelta = new Vector2(880, 260);
        picture.rectTransform.anchoredPosition = new Vector2(0, 240);
        picture.preserveAspect = true; picture.raycastTarget = false;

        titleText = NewText("Title", window.transform, "휴식처", 44, FontStyle.Bold, Ink, new Vector2(0, 70), new Vector2(880, 60));
        storyText = NewText("Story", window.transform, "", 24, FontStyle.Normal, InkLight, new Vector2(0, -20), new Vector2(860, 90));

        restButton = NewButton("Rest", window.transform, Green, out restLabel, OnRest);
        upgradeButton = NewButton("Upgrade", window.transform, Brown, out upgradeLabel, OnUpgrade);
        leaveButton = NewButton("Leave", window.transform, Ink, out Text leaveLabel, OnLeave);
        leaveLabel.text = "지나간다";
        ((RectTransform)leaveButton.transform).sizeDelta = new Vector2(300, 56);
    }

    private Button NewButton(string name, Transform parent, Color color, out Text label, System.Action onClick)
    {
        Image b = NewImage(name, parent, color);
        b.rectTransform.sizeDelta = new Vector2(840, 62);
        label = NewText("Label", b.transform, "", 24, FontStyle.Bold, Color.white, Vector2.zero, new Vector2(820, 62));
        Button button = b.gameObject.AddComponent<Button>();
        button.onClick.AddListener(() => onClick());
        return button;
    }

    private Image NewImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>(); image.color = color; return image;
    }

    private Text NewText(string name, Transform parent, string content, int size, FontStyle style, Color color, Vector2 pos, Vector2 box)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = font; text.text = content; text.fontSize = size; text.fontStyle = style; text.color = color;
        text.alignment = TextAnchor.MiddleCenter; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.raycastTarget = false;
        text.rectTransform.sizeDelta = box; text.rectTransform.anchoredPosition = pos;
        return text;
    }
}
