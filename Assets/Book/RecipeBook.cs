using System.Collections.Generic;
using UnityEngine;

// 알아낸 조합법을 영구히 기록한다 (게임을 껐다 켜도, 새 런을 시작해도 유지)
// 조합에 성공하면 RecipeBook.Unlock(재료A, 재료B) 한 줄만 부르면 된다
public static class RecipeBook
{
    private const string SaveKey = "unlocked_recipes";
    private static HashSet<string> unlocked;

    // 재료 순서가 바뀌어도 같은 조합으로 본다 (파이어볼+힐 = 힐+파이어볼)
    private static string Key(CardData a, CardData b)
    {
        string x = a != null ? a.name : "", y = b != null ? b.name : "";
        return string.CompareOrdinal(x, y) <= 0 ? x + "+" + y : y + "+" + x;
    }

    private static void Load()
    {
        if (unlocked != null) return;
        unlocked = new HashSet<string>();
        string saved = PlayerPrefs.GetString(SaveKey, "");
        foreach (string k in saved.Split('|'))
            if (!string.IsNullOrEmpty(k)) unlocked.Add(k);
    }

    public static bool IsUnlocked(CardData a, CardData b)
    {
        Load();
        return unlocked.Contains(Key(a, b));
    }

    // 처음 알아낸 조합이면 true
    public static bool Unlock(CardData a, CardData b)
    {
        Load();
        if (!unlocked.Add(Key(a, b))) return false;
        PlayerPrefs.SetString(SaveKey, string.Join("|", unlocked));
        PlayerPrefs.Save();
        Debug.Log($"새 조합법 해금: {(a != null ? a.cardName : "?")} + {(b != null ? b.cardName : "?")}");
        return true;
    }

    public static int UnlockedCount { get { Load(); return unlocked.Count; } }

    // 테스트용: 해금 기록 지우기
    public static void ResetAll()
    {
        unlocked = new HashSet<string>();
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        Debug.Log("조합법 해금 기록을 모두 지웠어요.");
    }
}