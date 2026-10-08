using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // TextMeshPro를 사용하는 경우 필수
using UnityEngine.UI; // 일반 UI Text를 사용하는 경우 필요

public class LoadingManager : MonoBehaviour
{
    [Header("씬 설정")]
    public string nextSceneName = "Main"; // 이동할 메인 씬 이름
    public float minLoadingTime = 3f;     // 최소 연출 시간 (초)

    [Header("UI 텍스트 연결 (둘 중 하나만 사용)")]
    public TMP_Text tmpProgressText; // TextMeshPro를 사용할 경우 연결
    public Text legacyProgressText;  // 일반 UI Text를 사용할 경우 연결

    void Start()
    {
        StartCoroutine(LoadNextScene());
    }

    IEnumerator LoadNextScene()
    {
        float timer = 0f;

        // 비동기 씬 로딩 시작
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            timer += Time.deltaTime;

            // 1. 유니티 asyncLoad.progress는 0.0 ~ 0.9까지 채워짐
            float loadProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            // 2. 최소 대기 시간(minLoadingTime) 비율 계산
            float timeProgress = Mathf.Clamp01(timer / minLoadingTime);

            // 3. 실제 로딩과 최소 시간 중 더 낮거나 자연스러운 진행률 선택 (0 ~ 1.0)
            float currentProgress = Mathf.Min(loadProgress, timeProgress);

            // 4. 퍼센트(0~100%)로 변환 후 텍스트 표시
            int displayPercentage = Mathf.RoundToInt(currentProgress * 100f);

            UpdateProgressText($"{displayPercentage}%");

            // 로딩 완료 및 최소 시간이 모두 지난 경우
            if (asyncLoad.progress >= 0.9f && timer >= minLoadingTime)
            {
                UpdateProgressText("100%");
                yield return new WaitForSeconds(0.2f); // 100%를 잠시 보여준 뒤 전환
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    // UI 종류에 맞춰 텍스트 업데이트
    void UpdateProgressText(string text)
    {
        if (tmpProgressText != null)
            tmpProgressText.text = text;

        if (legacyProgressText != null)
            legacyProgressText.text = text;
    }
}