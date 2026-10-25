using System.Collections.Generic;
using UnityEngine;

public class EncyclopediaManager : MonoBehaviour
{
    public static EncyclopediaManager Instance { get; private set; }

    // 해금된 카드 이름들을 저장하는 Set
    private HashSet<string> unlockedCardNames = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadUnlockedCards();
    }

    // 카드 해금 처리
    public void UnlockCard(string cardName)
    {
        if (string.IsNullOrEmpty(cardName)) return;

        if (!unlockedCardNames.Contains(cardName))
        {
            unlockedCardNames.Add(cardName);
            SaveUnlockedCards();
            Debug.Log($"[도감 해금] 새로운 카드가 도감에 등록되었습니다: {cardName}");
        }
    }

    // 해당 카드가 해금되었는지 확인
    public bool IsUnlocked(string cardName)
    {
        return unlockedCardNames.Contains(cardName);
    }

    // PlayerPrefs에 쉼표(,) 구분자로 저장
    private void SaveUnlockedCards()
    {
        string data = string.Join(",", unlockedCardNames);
        PlayerPrefs.SetString("UnlockedCardsData", data);
        PlayerPrefs.Save();
    }

    // 저장된 해금 데이터 불러오기
    private void LoadUnlockedCards()
    {
        unlockedCardNames.Clear();
        string data = PlayerPrefs.GetString("UnlockedCardsData", "");
        if (!string.IsNullOrEmpty(data))
        {
            string[] names = data.Split(',');
            foreach (string name in names)
            {
                if (!string.IsNullOrEmpty(name))
                    unlockedCardNames.Add(name);
            }
        }
    }
}