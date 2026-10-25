using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CurrencyHUD : MonoBehaviour
{
    public static CurrencyHUD Instance;

    [Header("UI References")]
    public TextMeshProUGUI goldText;
    public RectTransform goldIconRect; // 상단에 위치한 골드 아이콘의 RectTransform

    [Header("Animation Prefab")]
    public GameObject floatingIconPrefab; // 날아가는 골드 아이콘 이미지 프리팹 (UI Image)
    public Transform canvasTransform;     // UI Canvas Transform

    private void Awake()
    {
        // 씬이 넘어가도 이 HUD 오브젝트가 파괴되지 않고 계속 유지되도록 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // 중복 생성 방지
        }
    }

    private void Start()
    {
        UpdateGoldUI();
    }

    public void UpdateGoldUI()
    {
        if (GoldManager.Instance != null)
        {
            goldText.text = GoldManager.Instance.currentGold.ToString("N0");
        }
    }

    // 몬스터 위치에서 상단 HUD로 아이콘이 날아가는 연출
    public void PlayGoldFlyEffect(Vector3 startWorldPos, int goldAmount)
    {
        StartCoroutine(FlyEffectCoroutine(startWorldPos, goldAmount));
    }

    private IEnumerator FlyEffectCoroutine(Vector3 startWorldPos, int goldAmount)
    {
        if (floatingIconPrefab == null || canvasTransform == null)
        {
            // 프리팹이 없으면 즉시 반영
            GoldManager.Instance.AddGold(goldAmount);
            UpdateGoldUI();
            yield break;
        }

        // 1. 임시 아이콘 생성
        GameObject iconObj = Instantiate(floatingIconPrefab, canvasTransform);
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();

        // 월드 좌표를 Canvas UI 좌표로 변환
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(startWorldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasTransform as RectTransform,
            screenPoint,
            null,
            out Vector2 localPos
        );
        iconRect.anchoredPosition = localPos;

        // 2. 목표 지점 (상단 골드 아이콘 위치)
        Vector3 targetPos = goldIconRect.anchoredPosition;

        float duration = 0.6f;
        float elapsedTime = 0f;
        Vector3 startPos = iconRect.anchoredPosition;

        // 3. 부드럽게 이동 애니메이션
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            t = 1f - (1f - t) * (1f - t); // EaseOutQuad

            iconRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        iconRect.anchoredPosition = targetPos;

        // 4. 데이터 반영 및 UI 업데이트
        GoldManager.Instance.AddGold(goldAmount);
        UpdateGoldUI();

        // 5. 임시 오브젝트 파괴
        Destroy(iconObj);
    }
}