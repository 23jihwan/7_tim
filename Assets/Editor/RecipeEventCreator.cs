#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 메뉴: 마법사의 탑 → 조합법 이벤트 2개 만들기
// 1) 고대 석판(2단계 조합법), 금지된 마도서(3단계 조합법) 이벤트 파일을 만들고
// 2) 그림을 연결하고 3) 지금 열린 맵 씬의 EventPopup 목록에 넣고 Recipe Database도 채운다
public static class RecipeEventCreator
{
    [MenuItem("마법사의 탑/조합법 이벤트 2개 만들기")]
    private static void Create()
    {
        string folder = FindEventFolder();

        EventData tablet = MakeEvent(folder, "Event_AncientTablet", "고대 석판", "EventImage_AncientTablet",
            "벽에 박힌 낡은 석판에 희미하게 빛나는 마법 공식이 새겨져 있다.\n해독하면 새로운 조합법을 깨우칠 수 있을 것 같다.",
            new EventChoice { text = "골드를 들여 해독 도구를 쓴다", goldCost = 60, reward = EventRewardType.Recipe, recipeTier = 2,
                              resultText = "공식이 머릿속에 또렷하게 새겨졌다." },
            new EventChoice { text = "손을 베어 피로 룬을 깨운다", hpCost = 12, reward = EventRewardType.Recipe, recipeTier = 2,
                              trapChance = 0.25f, trapHpLoss = 8, resultText = "피에 반응한 룬이 비밀을 속삭였다." },
            new EventChoice { text = "지나간다", reward = EventRewardType.None, resultText = "석판의 빛이 조용히 사그라들었다." });

        EventData grimoire = MakeEvent(folder, "Event_ForbiddenGrimoire", "금지된 마도서", "EventImage_ForbiddenGrimoire",
            "쇠사슬에 묶인 검은 마도서가 낮게 속삭인다.\n펼치면 강력한 비밀을 알 수 있지만, 대가가 따를 것이다.",
            new EventChoice { text = "골드를 바쳐 봉인을 푼다", goldCost = 120, reward = EventRewardType.Recipe, recipeTier = 3,
                              resultText = "사슬이 풀리며 금지된 공식이 드러났다." },
            new EventChoice { text = "생명력을 바쳐 억지로 펼친다", hpCost = 25, reward = EventRewardType.Recipe, recipeTier = 3,
                              resultText = "마도서가 생명력을 빨아들이고 비밀을 내주었다." },
            new EventChoice { text = "외면하고 지나간다", reward = EventRewardType.None, resultText = "등 뒤에서 속삭임이 오래도록 들렸다." });

        AssetDatabase.SaveAssets();
        AddToEventPopup(tablet, grimoire);
        Debug.Log($"조합법 이벤트 2개를 만들었어요: {folder}");
    }

    private static EventData MakeEvent(string folder, string fileName, string title, string imageName, string story, params EventChoice[] choices)
    {
        string path = $"{folder}/{fileName}.asset";
        EventData ev = AssetDatabase.LoadAssetAtPath<EventData>(path);
        if (ev == null)
        {
            ev = ScriptableObject.CreateInstance<EventData>();
            AssetDatabase.CreateAsset(ev, path);
        }
        ev.title = title;
        ev.story = story;
        ev.choices = choices;
        ev.image = FindSprite(imageName);
        EditorUtility.SetDirty(ev);
        return ev;
    }

    private static void AddToEventPopup(params EventData[] newEvents)
    {
        EventPopup popup = Object.FindFirstObjectByType<EventPopup>(FindObjectsInactive.Include);
        if (popup == null)
        {
            Debug.LogWarning("열린 씬에 EventPopup이 없어요. 맵 씬을 열고 다시 누르거나, EventPopup의 Events 목록에 직접 넣어 주세요.");
            return;
        }

        Undo.RecordObject(popup, "조합법 이벤트 추가");
        List<EventData> list = new List<EventData>(popup.events ?? new EventData[0]);
        foreach (EventData e in newEvents)
            if (!list.Contains(e)) list.Add(e);
        popup.events = list.ToArray();

        if (popup.recipeDatabase == null)
        {
            string[] dbs = AssetDatabase.FindAssets("t:CardRecipeDatabase");
            if (dbs.Length > 0)
                popup.recipeDatabase = AssetDatabase.LoadAssetAtPath<CardRecipeDatabase>(AssetDatabase.GUIDToAssetPath(dbs[0]));
        }

        EditorUtility.SetDirty(popup);
        EditorSceneManager.MarkSceneDirty(popup.gameObject.scene);
        Debug.Log($"EventPopup 이벤트 목록에 추가했어요 (모두 {popup.events.Length}개). 씬을 저장해 주세요. (Ctrl + S)");
    }

    private static string FindEventFolder()
    {
        string[] found = AssetDatabase.FindAssets("t:EventData");
        if (found.Length > 0) return System.IO.Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(found[0])).Replace("\\", "/");
        if (!AssetDatabase.IsValidFolder("Assets/EventData")) AssetDatabase.CreateFolder("Assets", "EventData");
        return "Assets/EventData";
    }

    private static Sprite FindSprite(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:Sprite"))
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
            if (s != null && s.name == name) return s;
        }
        Debug.LogWarning($"그림 {name}을(를) 찾지 못했어요. Sprite (2D and UI)로 바꿨는지 확인해 주세요.");
        return null;
    }
}
#endif
