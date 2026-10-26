#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 메뉴: 마법사의 탑 → 효과음 자동 등록
// 프로젝트 안의 "SFX_이름" 소리 파일을 찾아 SoundLibrary의 효과음 목록에 "이름"으로 넣어 준다
// 예: SFX_click.wav → SoundManager.Play("click")
public static class SoundSetupTool
{
    [MenuItem("마법사의 탑/효과음 자동 등록")]
    private static void RegisterSfx()
    {
        string[] libs = AssetDatabase.FindAssets("t:SoundLibrary");
        if (libs.Length == 0)
        {
            Debug.LogWarning("SoundLibrary를 찾지 못했어요. Resources 폴더에 SoundLibrary가 있는지 확인해 주세요.");
            return;
        }
        SoundLibrary library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(AssetDatabase.GUIDToAssetPath(libs[0]));

        List<SoundLibrary.Sfx> list = new List<SoundLibrary.Sfx>(library.sfx ?? new SoundLibrary.Sfx[0]);
        int added = 0, updated = 0;

        foreach (string guid in AssetDatabase.FindAssets("SFX_ t:AudioClip"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!fileName.StartsWith("SFX_")) continue;

            string key = fileName.Substring(4);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

            SoundLibrary.Sfx existing = list.Find(s => s.name == key);
            if (existing != null)
            {
                if (existing.clip != clip) { existing.clip = clip; updated++; }   // 같은 이름이면 소리만 바꾼다 (볼륨은 유지)
            }
            else
            {
                list.Add(new SoundLibrary.Sfx { name = key, clip = clip, volume = 1f });
                added++;
            }
        }

        library.sfx = list.ToArray();
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        Debug.Log($"효과음 등록 완료: 새로 {added}개, 바뀐 것 {updated}개 (전체 {list.Count}개)");
    }
}
#endif
