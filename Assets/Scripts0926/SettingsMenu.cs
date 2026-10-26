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

    // 소리 크기가 바뀌면 알려준다 (0 = 전체, 1 = 배경음, 2 = 효과음 / 값은 0~1)
    // SoundManager가 이걸 듣고 실제 소리 크기를 바꾼다
    public static event Action<int, float> VolumeChanged;

    [Tooltip("톱니바퀴를 숨길 씬 (로딩 화면 등)")]
    public string[] hiddenInScenes = { "StartLoding", "MapLoding" };
    [Tooltip("메인 화면 씬 이름")]
    public string mainSceneName = "Main";

    // 저장 이름 (SoundManager와 같은 이름을 쓴다)
    public static readonly string[] VolumeKeys = { "vol_master", "vol_bgm", "vol_sfx" };
    public static readonly float[] VolumeDefaults = { 0.8f, 0.6f, 0.7f };
    private static readonly string[] VolumeLabels = { "전체 음량", "배경음", "효과음" };
    private static readonly Vector2Int[] Resolutions = { new Vector2Int(1920, 1080), new Vector2Int(1600, 900), new Vector2Int(1280, 720) };

    private static readonly Color Parchment = new Color32(0xEA, 0xDF, 0xC3, 0xFF);
    private static readonly Color ParchmentDark = new Color32(0xD9, 0xCB, 0xA5, 0xFF);
    private static readonly Color Ink = new Color32(0x3A, 0x2C, 0x1A, 0xFF);
    private static readonly Color Brown = new Color32(0x7A, 0x64, 0x48, 0xFF);
    private static readonly Color Red = new Color32(0x8F, 0x2A, 0x1E, 0xFF);
    private static readonly Color Track = new Color32(0xCB, 0xBB, 0x94, 0xFF);

    private Font font;
    private GameObject gearButton;
    private GameObject panel;
    private bool isOpen;

    // 소리 막대
    private readonly RectTransform[] volumeFills = new RectTransform[3];
    private readonly RectTransform[] volumeKnobs = new RectTransform[3];
    private readonly Text[] volumeTexts = new Text[3];
    private readonly float[] pendingVolumes = new float[3];   // 저장하기 전, 지금 조절 중인 값
    private bool volumeDirty;                                 // 저장하지 않은 변경이 있는지
    private Text statusText;

    // 화면 모드, 해상도
    private readonly Image[] modeButtons = new Image[2];
    private readonly Text[] modeTexts = new Text[2];
    private Text resolutionText;
    private int resolutionIndex;
    private Button mainButton;

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

        resolutionIndex = PlayerPrefs.GetInt("screen_res", 0);
        for (int i = 0; i < 3; i++) pendingVolumes[i] = GetVolume(i);
        BuildUI();
        RefreshAll();

        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Close();
        RefreshScene(scene.name);
    }

    private void RefreshScene(string sceneName)
    {
        gearButton.SetActive(Array.IndexOf(hiddenInScenes, sceneName) < 0);

        // 메인 화면에서는 "메인 화면으로" 버튼을 흐리게
        bool inMain = sceneName == mainSceneName;
        mainButton.interactable = !inMain;
        mainButton.GetComponent<Image>().color = inMain ? new Color(Brown.r, Brown.g, Brown.b, 0.35f) : Brown;
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
        SoundManager.Play("page");
        for (int i = 0; i < 3; i++) pendingVolumes[i] = GetVolume(i);   // 저장된 값에서 시작
        volumeDirty = false;
        SetStatus("", Brown);
        RefreshAll();
        panel.SetActive(true);
        Time.timeScale = 0f;           // 설정창이 열린 동안 게임을 멈춘다
    }

    public void Close()
    {
        // 저장하지 않고 닫으면 저장된 소리 크기로 되돌린다
        if (volumeDirty)
        {
            for (int i = 0; i < 3; i++)
            {
                pendingVolumes[i] = GetVolume(i);
                VolumeChanged?.Invoke(i, pendingVolumes[i]);
            }
            volumeDirty = false;
        }

        isOpen = false;
        if (panel != null) panel.SetActive(false);
        Time.timeScale = 1f;
    }

    // ---------------- 소리 ----------------

    public static float GetVolume(int channel)
    {
        return PlayerPrefs.GetFloat(VolumeKeys[channel], VolumeDefaults[channel]);
    }

    // - + 버튼: 10%씩
    private void ChangeVolume(int channel, float delta)
    {
        SetPendingVolume(channel, Mathf.Round((pendingVolumes[channel] + delta) * 10f) / 10f);
    }

    // 막대를 끌거나 눌렀을 때, 그리고 - + 버튼: 미리 들려주기만 하고 아직 저장하지 않는다
    private void SetPendingVolume(int channel, float value)
    {
        pendingVolumes[channel] = Mathf.Clamp01(Mathf.Round(value * 100f) / 100f);
        VolumeChanged?.Invoke(channel, pendingVolumes[channel]);
        volumeDirty = true;
        SetStatus("저장하지 않으면 창을 닫을 때 이전 소리로 돌아가요", Red);
        RefreshVolume(channel);
    }

    private void SaveSettings()
    {
        for (int i = 0; i < 3; i++) PlayerPrefs.SetFloat(VolumeKeys[i], pendingVolumes[i]);
        PlayerPrefs.Save();
        volumeDirty = false;
        SetStatus("저장되었어요", Brown);
    }

    private void SetStatus(string message, Color color)
    {
        if (statusText == null) return;
        statusText.text = message;
        statusText.color = color;
    }

    private void RefreshVolume(int channel)
    {
        float v = pendingVolumes[channel];
        volumeFills[channel].anchorMax = new Vector2(v, 1f);
        volumeKnobs[channel].anchorMin = volumeKnobs[channel].anchorMax = new Vector2(v, 0.5f);
        volumeTexts[channel].text = Mathf.RoundToInt(v * 100f) + "%";
    }

    // ---------------- 화면 ----------------

    private void SetFullScreen(bool full)
    {
        Screen.fullScreenMode = full ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        PlayerPrefs.SetInt("screen_full", full ? 1 : 0);
        ApplyResolution();
        RefreshScreen(full);
    }

    private void ChangeResolution(int step)
    {
        resolutionIndex = (resolutionIndex + step + Resolutions.Length) % Resolutions.Length;
        PlayerPrefs.SetInt("screen_res", resolutionIndex);
        ApplyResolution();
        RefreshScreen(Screen.fullScreenMode != FullScreenMode.Windowed);
    }

    private void ApplyResolution()
    {
        Vector2Int r = Resolutions[resolutionIndex];
        Screen.SetResolution(r.x, r.y, Screen.fullScreenMode);   // 에디터에서는 적용되지 않고, 빌드한 게임에서 적용된다
        PlayerPrefs.Save();
    }

    private void RefreshScreen(bool full)
    {
        for (int i = 0; i < 2; i++)
        {
            bool on = (i == 0) == full;
            modeButtons[i].color = on ? Ink : ParchmentDark;
            modeTexts[i].color = on ? Parchment : Ink;
        }
        Vector2Int r = Resolutions[resolutionIndex];
        resolutionText.text = $"{r.x} × {r.y}";
    }

    private void RefreshAll()
    {
        for (int i = 0; i < 3; i++) RefreshVolume(i);
        RefreshScreen(Screen.fullScreenMode != FullScreenMode.Windowed);
    }

    // ---------------- 버튼 ----------------

    private void GoToMain()
    {
        Close();
        if (RunState.Instance != null) RunState.Instance.NewRun();   // 진행 중이던 런은 처음부터
        SceneManager.LoadScene(mainSceneName);
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
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        // 1. 오른쪽 위 톱니바퀴 (인벤토리 아이콘 왼쪽)
        Image gear = NewImage("GearButton", transform, Color.white);
        RectTransform gearRt = gear.rectTransform;
        gearRt.anchorMin = gearRt.anchorMax = new Vector2(0f, 1f);   // 왼쪽 위 기준
        gearRt.pivot = new Vector2(0f, 1f);
        gearRt.sizeDelta = new Vector2(72, 72);
        gearRt.anchoredPosition = new Vector2(28, -28);
        Sprite gearSprite = Resources.Load<Sprite>("SettingsGear");
        if (gearSprite != null) gear.sprite = gearSprite; else gear.color = Brown;
        Shadow shadow = gear.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.6f);
        shadow.effectDistance = new Vector2(3, -3);
        gear.gameObject.AddComponent<Button>().onClick.AddListener(() => { if (isOpen) Close(); else Open(); });
        gearButton = gear.gameObject;

        // 2. 어두운 배경 + 창
        Image dim = NewImage("SettingsPanel", transform, new Color(0, 0, 0, 0.62f));
        RectTransform dimRt = dim.rectTransform;
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        panel = dim.gameObject;

        Image window = NewImage("Window", dim.transform, Parchment);
        window.rectTransform.sizeDelta = new Vector2(780, 720);
        Outline border = window.gameObject.AddComponent<Outline>();
        border.effectColor = Ink;
        border.effectDistance = new Vector2(4, -4);
        Transform w = window.transform;

        NewText("Title", w, "설정", 48, FontStyle.Bold, Ink, TextAnchor.MiddleCenter, new Vector2(0, 300), new Vector2(600, 70));
        NewText("Hint", w, "ESC를 누르면 닫힙니다", 18, FontStyle.Normal, Brown, TextAnchor.MiddleCenter, new Vector2(0, 255), new Vector2(600, 30));
        NewButton("CloseButton", w, "X", Ink, Parchment, new Vector2(350, 320), new Vector2(48, 48), 24, Close);

        // 3. 소리
        NewSection(w, "소리", 200);
        for (int i = 0; i < 3; i++)
        {
            int ch = i;
            float y = 140 - i * 50;
            NewText("Label", w, VolumeLabels[i], 22, FontStyle.Bold, Ink, TextAnchor.MiddleLeft, new Vector2(-272, y), new Vector2(140, 40));
            NewButton("Minus", w, "-", Brown, Color.white, new Vector2(-130, y), new Vector2(40, 40), 26, () => ChangeVolume(ch, -0.1f));
            NewButton("Plus", w, "+", Brown, Color.white, new Vector2(230, y), new Vector2(40, 40), 26, () => ChangeVolume(ch, 0.1f));

            // 마우스로 누르고 끌 수 있는 투명한 영역 (막대보다 위아래로 넉넉하게)
            Image hit = NewImage("DragArea", w, new Color(0, 0, 0, 0));
            hit.rectTransform.sizeDelta = new Vector2(300, 40);
            hit.rectTransform.anchoredPosition = new Vector2(50, y);
            hit.gameObject.AddComponent<VolumeDragArea>().onValueChanged = v => SetPendingVolume(ch, v);

            Image track = NewImage("Track", w, Track);
            track.rectTransform.sizeDelta = new Vector2(300, 16);
            track.rectTransform.anchoredPosition = new Vector2(50, y);
            track.raycastTarget = false;
            Outline trackLine = track.gameObject.AddComponent<Outline>();
            trackLine.effectColor = Ink;
            trackLine.effectDistance = new Vector2(2, -2);

            Image fill = NewImage("Fill", track.transform, Red);
            fill.raycastTarget = false;
            RectTransform fillRt = fill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0.5f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            volumeFills[i] = fillRt;

            Image knob = NewImage("Knob", track.transform, Parchment);
            knob.raycastTarget = false;
            RectTransform knobRt = knob.rectTransform;
            knobRt.anchorMin = knobRt.anchorMax = new Vector2(0.5f, 0.5f);
            knobRt.sizeDelta = new Vector2(22, 22);
            knobRt.anchoredPosition = Vector2.zero;
            knobRt.localRotation = Quaternion.Euler(0, 0, 45);
            Outline knobLine = knob.gameObject.AddComponent<Outline>();
            knobLine.effectColor = Ink;
            knobLine.effectDistance = new Vector2(2, -2);
            volumeKnobs[i] = knobRt;

            volumeTexts[i] = NewText("Value", w, "80%", 22, FontStyle.Bold, Ink, TextAnchor.MiddleRight, new Vector2(302, y), new Vector2(80, 40));
        }

        // 4. 화면
        NewSection(w, "화면", -20);
        NewText("Label", w, "화면 모드", 22, FontStyle.Bold, Ink, TextAnchor.MiddleLeft, new Vector2(-272, -80), new Vector2(140, 40));
        string[] modes = { "전체화면", "창모드" };
        for (int i = 0; i < 2; i++)
        {
            bool full = i == 0;
            Button b = NewButton("Mode", w, modes[i], ParchmentDark, Ink, new Vector2(i == 0 ? -69 : 211, -80), new Vector2(262, 46), 21, () => SetFullScreen(full));
            modeButtons[i] = b.GetComponent<Image>();
            modeTexts[i] = b.GetComponentInChildren<Text>();
        }

        NewText("Label", w, "해상도", 22, FontStyle.Bold, Ink, TextAnchor.MiddleLeft, new Vector2(-272, -140), new Vector2(140, 40));
        NewButton("Prev", w, "<", Brown, Color.white, new Vector2(-178, -140), new Vector2(44, 44), 22, () => ChangeResolution(-1));
        NewButton("Next", w, ">", Brown, Color.white, new Vector2(320, -140), new Vector2(44, 44), 22, () => ChangeResolution(1));
        Image resBox = NewImage("ResolutionBox", w, new Color32(0xF4, 0xEC, 0xD8, 0xFF));
        resBox.rectTransform.sizeDelta = new Vector2(430, 44);
        resBox.rectTransform.anchoredPosition = new Vector2(71, -140);
        Outline resLine = resBox.gameObject.AddComponent<Outline>();
        resLine.effectColor = Ink;
        resLine.effectDistance = new Vector2(2, -2);
        resolutionText = NewText("Value", resBox.transform, "1920 × 1080", 22, FontStyle.Bold, Ink, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(430, 44));

        // 5. 저장 안내 글자
        statusText = NewText("Status", w, "", 18, FontStyle.Bold, Brown, TextAnchor.MiddleCenter, new Vector2(0, -205), new Vector2(684, 30));

        // 6. 아래 버튼 4개
        NewButton("SaveButton", w, "저장", Ink, Parchment, new Vector2(-261, -270), new Vector2(160, 64), 24, SaveSettings);
        NewButton("ContinueButton", w, "계속하기", Brown, Color.white, new Vector2(-87, -270), new Vector2(160, 64), 22, Close);
        mainButton = NewButton("MainButton", w, "메인 화면으로", Brown, Color.white, new Vector2(87, -270), new Vector2(160, 64), 20, GoToMain);
        NewButton("QuitButton", w, "게임 종료", Red, Color.white, new Vector2(261, -270), new Vector2(160, 64), 22, QuitGame);

        panel.SetActive(false);
    }

    private void NewSection(Transform parent, string title, float y)
    {
        NewText("Section", parent, title, 18, FontStyle.Bold, Brown, TextAnchor.MiddleLeft, new Vector2(0, y), new Vector2(684, 30));
        Image line = NewImage("Line", parent, new Color32(0xC9, 0xB8, 0x8F, 0xFF));
        line.rectTransform.sizeDelta = new Vector2(684, 2);
        line.rectTransform.anchoredPosition = new Vector2(0, y - 20);
        line.raycastTarget = false;
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

    private Button NewButton(string name, Transform parent, string label, Color bg, Color textColor, Vector2 pos, Vector2 size, int fontSize, Action onClick)
    {
        Image image = NewImage(name, parent, bg);
        image.rectTransform.sizeDelta = size;
        image.rectTransform.anchoredPosition = pos;
        NewText("Label", image.transform, label, fontSize, FontStyle.Bold, textColor, TextAnchor.MiddleCenter, Vector2.zero, size);
        Button button = image.gameObject.AddComponent<Button>();
        button.onClick.AddListener(() => { SoundManager.Play("click"); onClick(); });
        return button;
    }
}
