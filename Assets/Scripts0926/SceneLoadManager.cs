using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수적인 네임스페이스

public class SceneLoadManager : MonoBehaviour
{
    // 1. 씬 이름을 문자로 입력받아 이동하는 함수 (UI 버튼 Inspector에서 사용)
    public void LoadSceneByName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("이동할 씬 이름이 입력되지 않았습니다!");
        }
    }

    // 2. Build Settings의 씬 번호(Index)로 이동하는 함수
    public void LoadSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }

    // 3. 현재 씬을 처음부터 다시 시작하는 함수 (전투 리셋용)
    public void ReloadCurrentScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }
}