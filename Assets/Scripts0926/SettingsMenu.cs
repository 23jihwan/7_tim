using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 모든 씬의 오른쪽 위에 톱니바퀴 버튼을 띄우고, 누르거나 ESC를 누르면 설정창을 연다
// 게임이 시작되면 자동으로 만들어지므로 씬에 직접 놓을 필요는 없다
public class SettingsMenu : MonoBehaviour
{
    public static SettingsMenu Instance { get; private set; }

    [Tooltip("톱니바퀴를 숨길 씬 (로딩 화면 등)")]
    public string[] hiddenInScenes = { "StartLoding", "MapLoding" };

    private GameObject gearButton;
    private GameObject panel;
    private bool isOpen;

    private static readonly Color Parchment = new Color32(0xEA, 0xDF, 0xC3, 0xFF);
    private static readonly Color Ink = new Color32(0x3A, 0x2C, 0x1A, 0xFF);
    private static readonly Color Brown = new Color32(0x7A, 0x64, 0x48, 0xFF);
    private static readonly Color Red = new Color32(0x8F, 0x2A, 0x1E, 0xFF);

    // 게임이 시작될 때 자동으로 하나 만든다
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance == null) new GameObject("SettingsMenu").AddComponent<SettingsMenu>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildUI();
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshGear(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Close();                       // 씬이 바뀌면 설정창은 닫고 시간도 다시 흐르게
        RefreshGear(scene.name);
    }

    private void RefreshGear(string sceneName)
    {
        bool hidden = Array.IndexOf(hiddenInScenes, sceneName) >= 0;
        gearButton.SetActive(!hidden);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && gearButton.activeSelf)
        {
            if (isOpen) Close(); else Open();
        }
    }

    // ---------------- 열기 / 닫기 ----------------

    public void Open()
    {
        isOpen = true;
        panel.SetActive(true);
        Time.timeScale = 0f;           // 설정창이 열린 동안 게임을 멈춘다
    }

    public void Close()
    {
        isOpen = false;
        if (panel != null) panel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void QuitGame()
    {
        Debug.Log("게임을 종료합니다.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;   // 에디터에서는 Play를 끈다
#else
        Application.Quit();                                 // 실제 게임에서는 프로그램을 끈다
#endif
    }

    // ---------------- 화면 만들기 ----------------

    private void BuildUI()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 다른 모든 UI보다 위에 그려지는 Canvas
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        // 1. 오른쪽 위 톱니바퀴 버튼
        Image gear = NewImage("GearButton", transform, Color.white);
        RectTransform gearRt = gear.rectTransform;
        gearRt.anchorMin = gearRt.anchorMax = new Vector2(1f, 1f);
        gearRt.pivot = new Vector2(1f, 1f);
        gearRt.sizeDelta = new Vector2(72, 72);
        gearRt.anchoredPosition = new Vector2(-28, -28);
        Sprite gearSprite = Resources.Load<Sprite>("SettingsGear");
        if (gearSprite != null) gear.sprite = gearSprite;
        else gear.color = Brown;       // 아이콘 그림이 없으면 갈색 네모로 표시
        gear.raycastTarget = true;
        Shadow shadow = gear.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.6f);
        shadow.effectDistance = new Vector2(3, -3);
        gear.gameObject.AddComponent<Button>().onClick.AddListener(() => { if (isOpen) Close(); else Open(); });
        gearButton = gear.gameObject;

        // 2. 설정창 (어두운 배경 + 창)
        Image dim = NewImage("SettingsPanel", transform, new Color(0, 0, 0, 0.6f));
        RectTransform dimRt = dim.rectTransform;
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        panel = dim.gameObject;

        Image window = NewImage("Window", dim.transform, Parchment);
        window.rectTransform.sizeDelta = new Vector2(560, 420);
        Outline border = window.gameObject.AddComponent<Outline>();
        border.effectColor = Ink;
        border.effectDistance = new Vector2(4, -4);

        NewText("Title", window.transform, font, "설정", 48, FontStyle.Bold, Ink, new Vector2(0, 150), new Vector2(480, 70));
        NewText("Hint", window.transform, font, "ESC를 누르면 닫힙니다", 20, FontStyle.Normal, Brown, new Vector2(0, 98), new Vector2(480, 30));

        NewButton("ContinueButton", window.transform, font, "계속하기", Brown, new Vector2(0, 10), Close);
        NewButton("QuitButton", window.transform, font, "게임 종료", Red, new Vector2(0, -90), QuitGame);

        // 오른쪽 위 작은 X 버튼
        Button closeX = NewButton("CloseButton", window.transform, font, "X", Ink, new Vector2(240, 170), Close);
        closeX.GetComponent<RectTransform>().sizeDelta = new Vector2(48, 48);

        panel.SetActive(false);
    }

    private Image NewImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private Text NewText(string name, Transform parent, Font font, string content, int size, FontStyle style, Color color, Vector2 pos, Vector2 box)
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

    private Button NewButton(string name, Transform parent, Font font, string label, Color color, Vector2 pos, Action onClick)
    {
        Image image = NewImage(name, parent, color);
        image.rectTransform.sizeDelta = new Vector2(300, 72);
        image.rectTransform.anchoredPosition = pos;
        NewText("Label", image.transform, font, label, 30, FontStyle.Bold, Color.white, Vector2.zero, new Vector2(300, 72));
        Button button = image.gameObject.AddComponent<Button>();
        button.onClick.AddListener(() => onClick());
        return button;
    }
}