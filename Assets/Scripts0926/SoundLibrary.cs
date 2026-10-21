using System;
using UnityEngine;
using UnityEngine.Audio;

// 게임에서 쓰는 소리를 한곳에 모아 두는 데이터 파일
// 반드시 Resources 폴더 안에 "SoundLibrary"라는 이름으로 만들어야 SoundManager가 찾을 수 있다
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "마법사의 탑/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [Serializable]
    public class SceneBgm
    {
        [Tooltip("씬 이름 (Scenes 폴더의 이름과 똑같이)")]
        public string sceneName;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Serializable]
    public class Sfx
    {
        [Tooltip("코드에서 부를 이름. 예: SoundManager.Play(\"click\")")]
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Header("Audio Mixer")]
    public AudioMixer mixer;
    public AudioMixerGroup bgmGroup;
    public AudioMixerGroup sfxGroup;

    [Header("씬별 배경음악 (목록에 없는 씬은 앞의 음악을 계속 튼다)")]
    public SceneBgm[] sceneBgms;

    [Header("효과음")]
    public Sfx[] sfx;
}