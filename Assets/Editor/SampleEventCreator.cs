#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 메뉴 한 번으로 샘플 이벤트 6개를 만들어 주는 에디터 도구
// 메뉴: 마법사의 탑 → 샘플 이벤트 6개 만들기
public static class SampleEventCreator
{
    private const string Folder = "Assets/EventData";

    [MenuItem("마법사의 탑/샘플 이벤트 6개 만들기")]
    private static void CreateAll()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "EventData");
        List<EventData> created = new List<EventData>();

        created.Add(Make("Event_GhostMerchant", "유령 상인",
            "희미하게 일렁이는 상인이 낡은 진열대를 펼친다.\n\"살아 있는 자의 금화라면 무엇이든 팔지...\"",
            Choice("골드 50을 내고 유물을 산다", gold: 50, reward: EventRewardType.Relic, result: "상인이 소리 없이 웃으며 유물을 건넨다."),
            Choice("골드 25를 내고 주문서를 산다", gold: 25, reward: EventRewardType.Card, result: "빛바랜 주문서 몇 장을 펼쳐 보인다."),
            Choice("지나간다", result: "상인은 안개처럼 흩어졌다.")));

        created.Add(Make("Event_SealedAltar", "봉인된 제단",
            "붉은 룬이 새겨진 제단이 피를 원하듯 맥동한다.\n제단 위에는 무언가 봉인되어 있다.",
            Choice("피를 바친다", hp: 15, reward: EventRewardType.Relic, result: "봉인이 풀리며 유물이 모습을 드러낸다."),
            Choice("금 장식만 떼어 간다", reward: EventRewardType.Gold, goldMin: 15, goldMax: 25, trap: 0.25f, trapLoss: 8, result: "장식을 떼어 내자 제단이 분노한 듯 번쩍였다."),
            Choice("지나간다", result: "룬의 붉은 빛이 천천히 꺼졌다.")));

        created.Add(Make("Event_ElementalSpring", "원소의 샘",
            "네 가지 빛깔로 일렁이는 샘물이 마력을 뿜어낸다.\n불, 물, 바람, 얼음의 기운이 뒤섞여 있다.",
            Choice("샘물에 손을 담근다", reward: EventRewardType.Card, trap: 0.2f, trapLoss: 6, result: "샘이 새로운 주문을 속삭인다."),
            Choice("금화를 던져 소원을 빈다", gold: 20, reward: EventRewardType.Gold, goldMin: 25, goldMax: 60, result: "샘물이 반짝이며 금화를 돌려보냈다."),
            Choice("지나간다", result: "샘물 소리가 등 뒤로 멀어졌다.")));

        created.Add(Make("Event_ApprenticeBag", "견습생의 가방",
            "계단 구석에 견습 마법사의 가방이 떨어져 있다.\n주인은 어디에도 보이지 않는다.",
            Choice("가방을 뒤진다", reward: EventRewardType.Gold, goldMin: 20, goldMax: 35, result: "가방 바닥에서 금화 주머니를 찾았다."),
            Choice("안에 든 마법서를 읽는다", reward: EventRewardType.Card, result: "서툰 글씨로 적힌 주문을 익혔다."),
            Choice("그대로 둔다", result: "언젠가 주인이 찾으러 오기를 바라며 지나쳤다.")));

        created.Add(Make("Event_MirrorOfTruth", "진실의 거울",
            "거울 속의 내가 먼저 손을 내민다.\n\"네 힘을 조금 나눠 주면, 내가 아는 주문을 주지.\"",
            Choice("손을 맞잡는다", hp: 8, reward: EventRewardType.Card, result: "거울 속의 내가 주문을 속삭이고 사라졌다."),
            Choice("거울을 깨뜨린다", reward: EventRewardType.Gold, goldMin: 30, goldMax: 50, trap: 0.5f, trapLoss: 12, result: "파편 사이로 금화가 쏟아졌다."),
            Choice("눈을 피한다", result: "거울 속의 나는 아쉬운 듯 웃었다.")));

        created.Add(Make("Event_AbandonedLibrary", "버려진 서고",
            "먼지 쌓인 책장 사이에서 아직 마력이 남은 책 한 권이 빛난다.\n책장 뒤에는 작은 금고도 보인다.",
            Choice("빛나는 책을 펼친다", reward: EventRewardType.Card, result: "책장이 넘어가며 새로운 주문이 떠올랐다."),
            Choice("금고를 연다", reward: EventRewardType.Gold, goldMin: 25, goldMax: 45, trap: 0.3f, trapLoss: 10, result: "녹슨 자물쇠가 끼익 소리를 내며 열렸다."),
            Choice("지나간다", result: "서고의 정적이 다시 내려앉았다.")));

        AssetDatabase.SaveAssets();

        // 열려 있는 씬에 EventPopup이 있으면 목록에 자동으로 추가
        EventPopup popup = Object.FindFirstObjectByType<EventPopup>();
        if (popup != null)
        {
            Undo.RecordObject(popup, "샘플 이벤트 추가");
            List<EventData> list = new List<EventData>(popup.events ?? new EventData[0]);
            foreach (EventData e in created)
                if (e != null && !list.Contains(e)) list.Add(e);
            popup.events = list.ToArray();
            EditorUtility.SetDirty(popup);
            EditorSceneManager.MarkSceneDirty(popup.gameObject.scene);
            Debug.Log($"샘플 이벤트를 만들고 EventPopup 목록에 추가했어요. (지금 {popup.events.Length}개) 씬을 저장해 주세요.");
        }
        else
        {
            Debug.Log("샘플 이벤트를 만들었어요. EventData 폴더의 파일을 EventPopup의 Events에 넣어 주세요.");
        }
    }

    // EventImage_이름.png 그림을 Event_이름 이벤트에 자동으로 연결한다
    [MenuItem("마법사의 탑/이벤트 그림 자동 연결")]
    private static void LinkImages()
    {
        int linked = 0;
        foreach (string guid in AssetDatabase.FindAssets("EventImage_ t:Texture2D"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // 그림을 UI용 Sprite (Single)로 설정
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }

            string key = System.IO.Path.GetFileNameWithoutExtension(path).Replace("EventImage_", "");
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            EventData data = AssetDatabase.LoadAssetAtPath<EventData>($"{Folder}/Event_{key}.asset");
            if (sprite == null || data == null) continue;

            data.image = sprite;
            EditorUtility.SetDirty(data);
            linked++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"이벤트 그림 {linked}개를 연결했어요.");
    }

    private static EventData Make(string fileName, string title, string story, params EventChoice[] choices)
    {
        string path = $"{Folder}/{fileName}.asset";
        EventData existing = AssetDatabase.LoadAssetAtPath<EventData>(path);
        if (existing != null)
        {
            Debug.Log($"{fileName}은 이미 있어서 건너뛰었어요.");
            return existing;
        }

        EventData data = ScriptableObject.CreateInstance<EventData>();
        data.title = title;
        data.story = story;
        data.choices = choices;
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    private static EventChoice Choice(string text, int gold = 0, int hp = 0,
        EventRewardType reward = EventRewardType.None, int goldMin = 20, int goldMax = 40,
        float trap = 0f, int trapLoss = 10, string result = "")
    {
        return new EventChoice
        {
            text = text,
            goldCost = gold,
            hpCost = hp,
            reward = reward,
            goldMin = goldMin,
            goldMax = goldMax,
            trapChance = trap,
            trapHpLoss = trapLoss,
            resultText = result
        };
    }
}
#endif