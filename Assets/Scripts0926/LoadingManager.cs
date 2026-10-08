using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // TextMeshPro를 사용할 경우

public class LoadingManager : MonoBehaviour
{
    [Header("UI 연결")]
    public Image progressBar;          // 양피지 게이지 Fill 이미지
    public TextMeshProUGUI loadingText; // "던전 지도 불러오는 중..." 텍스트

    [Header("씬 설정")]
    public string nextSceneName = "DungeonMapScene"; // 이동할 다음 씬 이름
    public float minimumLoadingTime = 2.0f;          // 게이지 연출을 위한 최소 로딩 시간(초)

    private void Start()
    {
        StartCoroutine(LoadNextSceneAsync());
    }

    private IEnumerator LoadNextSceneAsync()
    {
        // 1. 비동기 씬 로딩 시작 (자동 전환 일단 대기)
        AsyncOperation op = SceneManager.LoadSceneAsync(nextSceneName);
        op.allowSceneActivation = false;

        float timer = 0f;

        // 2. 씬 로딩 및 최소 시간 동안 게이지 채우기 연출
        while (!op.isDone)
        {
            yield return null;
            timer += Time.deltaTime;

            // 실제 로딩 진행도 (0 ~ 0.9)를 0 ~ 1.0 비율로 변환
            float realProgress = Mathf.Clamp01(op.progress / 0.9f);

            // 너무 빨리 로딩되더라도 연출이 자연스럽게 차오르도록 보정
            float timeProgress = timer / minimumLoadingTime;
            float fillProgress = Mathf.Min(realProgress, timeProgress);

            // 게이지 바 Fill 업데이트
            if (progressBar != null)
            {
                progressBar.fillAmount = fillProgress;
            }

            // '...' 텍스트 애니메이션
            if (loadingText != null)
            {
                int dotCount = (int)(timer * 3) % 4; // 0, 1, 2, 3 순환
                string dots = new string('.', dotCount);
                loadingText.text = "던전 지도 불러오는 중" + dots;
            }

            // 로딩도 완료되고 최소 연출 시간도 채워지면 씬 전환 허용
            if (op.progress >= 0.9f && timer >= minimumLoadingTime)
            {
                // 완전히 게이지를 채운 후 씬 전환
                if (progressBar != null) progressBar.fillAmount = 1.0f;
                yield return new WaitForSeconds(0.2f); // 자연스러운 마무리를 위한 미세 대기

                op.allowSceneActivation = true; // 화면 자동 전환!
            }
        }
    }
}