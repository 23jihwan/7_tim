#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// 메뉴: 마법사의 탑 → 도감·조합법 기록 초기화
// 테스트하다 쌓인 도감 해금(= 아는 조합법)과 조합법 기록을 지운다. 소리·화면 설정은 그대로 둔다
public static class ProgressResetTool
{
    [MenuItem("마법사의 탑/도감·조합법 기록 초기화")]
    private static void ResetProgress()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("초기화", "Play를 끈 상태에서 눌러 주세요.", "확인");
            return;
        }
        if (!EditorUtility.DisplayDialog("도감·조합법 기록 초기화",
            "도감에 해금된 카드와 알고 있는 조합법을 모두 지울까요?\n(소리·화면 설정은 그대로예요)", "지우기", "취소"))
            return;

        PlayerPrefs.DeleteKey("UnlockedCardsData");   // 팀원 도감(EncyclopediaManager) 기록
        PlayerPrefs.DeleteKey("unlocked_recipes");    // 덱 위 조합법 창(RecipeBook) 기록
        PlayerPrefs.Save();
        Debug.Log("도감·조합법 기록을 지웠어요. 이제 Play하면 아무 조합법도 모르는 상태로 시작해요.");
    }
}
#endif