using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

// 소리 크기 종류
public enum SoundChannel { Master, BGM, SFX }

// 배경음악과 효과음을 재생하고, 소리 크기를 저장·적용한다
// 게임이 시작되면 자동으로 만들어지므로 씬에 직접 놓을 필요는 없다
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private SoundLibrary library;
    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private Coroutine fadeRoutine;
    private static readonly System.Collections.Generic.HashSet<string> warned = new System.Collections.Generic.HashSet<string>();

    // Audio Mixer에서 Expose한 파라미터 이름
    private static readonly string[] MixerParams = { "MasterVolume", "BGMVolume", "SFXVolume" };
    // 게임을 껐다 켜도 남도록 저장할 때 쓰는 이름
    private static readonly string[] SaveKeys = { "vol_master", "vol_bgm", "vol_sfx" };
    private static readonly float[] Defaults = { 0.8f, 0.6f, 0.7f };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance == null) new GameObject("SoundManager").AddComponent<SoundManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        library = Resources.Load<SoundLibrary>("SoundLibrary");
        if (library == null)
            Debug.LogWarning("SoundManager: Resources 폴더에서 SoundLibrary를 찾지 못했어요.");

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        if (library != null)
        {
            bgmSource.outputAudioMixerGroup = library.bgmGroup;
            sfxSource.outputAudioMixerGroup = library.sfxGroup;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        SettingsMenu.VolumeChanged += OnVolumeChanged;   // 설정창에서 소리 크기를 바꾸면 바로 적용
    }

    private void OnVolumeChanged(int channel, float value)
    {
        ApplyVolume((SoundChannel)channel, value);
    }

    private void Start()
    {
        // Audio Mixer 값은 Awake에서 바꾸면 적용이 안 될 수 있어서 Start에서 적용한다
        for (int i = 0; i < 3; i++) ApplyVolume((SoundChannel)i, GetVolume((SoundChannel)i));
        PlaySceneBgm(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SettingsMenu.VolumeChanged -= OnVolumeChanged;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlaySceneBgm(scene.name);
    }

    // ---------------- 배경음악 ----------------

    private void PlaySceneBgm(string sceneName)
    {
        if (library == null || library.sceneBgms == null) return;

        foreach (SoundLibrary.SceneBgm entry in library.sceneBgms)
        {
            if (entry.sceneName != sceneName || entry.clip == null) continue;
            if (bgmSource.clip == entry.clip && bgmSource.isPlaying) return;   // 같은 음악이면 끊지 않고 계속

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeTo(entry.clip, entry.volume));
            return;
        }
        // 목록에 없는 씬은 지금 음악을 그대로 둔다
    }

    // 지금 음악을 줄이고, 새 음악을 키운다 (설정창이 게임을 멈춰도 동작하게 unscaled 시간 사용)
    private IEnumerator FadeTo(AudioClip clip, float targetVolume)
    {
        const float duration = 0.6f;

        float start = bgmSource.volume;
        for (float t = 0; t < duration && bgmSource.isPlaying; t += Time.unscaledDeltaTime)
        {
            bgmSource.volume = Mathf.Lerp(start, 0f, t / duration);
            yield return null;
        }

        bgmSource.clip = clip;
        bgmSource.volume = 0f;
        bgmSource.Play();

        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            bgmSource.volume = Mathf.Lerp(0f, targetVolume, t / duration);
            yield return null;
        }
        bgmSource.volume = targetVolume;
        fadeRoutine = null;
    }

    // ---------------- 효과음 ----------------

    // 어디서든 SoundManager.Play("click") 처럼 부르면 된다
    public static void Play(string name)
    {
        if (Instance == null || Instance.library == null || Instance.library.sfx == null) return;

        foreach (SoundLibrary.Sfx s in Instance.library.sfx)
        {
            if (s.name == name && s.clip != null)
            {
                Instance.sfxSource.PlayOneShot(s.clip, s.volume);
                return;
            }
        }
        if (warned.Add(name)) Debug.LogWarning($"SoundManager: '{name}' 효과음이 Sound Library에 없어요.");
    }

    // ---------------- 소리 크기 ----------------

    // 0 ~ 1 사이 값
    public static float GetVolume(SoundChannel channel)
    {
        int i = (int)channel;
        return PlayerPrefs.GetFloat(SaveKeys[i], Defaults[i]);
    }

    public static void SetVolume(SoundChannel channel, float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SaveKeys[(int)channel], value);
        PlayerPrefs.Save();
        if (Instance != null) Instance.ApplyVolume(channel, value);
    }

    private void ApplyVolume(SoundChannel channel, float value)
    {
        if (library == null || library.mixer == null) return;
        // Audio Mixer는 데시벨(dB) 단위라서 0~1 값을 바꿔서 넣는다 (0이면 완전히 끔)
        float db = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
        library.mixer.SetFloat(MixerParams[(int)channel], db);
    }
}
