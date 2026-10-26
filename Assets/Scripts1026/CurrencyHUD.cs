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

    private int shownGold = -1;   // 지금 화면에 보이는 골드 (바뀌었을 때만 글자를 고친다)

    private void Awake()
    {
        // 골드는 RunState에 있으므로 HUD는 씬마다 하나씩 있으면 된다 (씬을 넘어 유지할 필요 없음)
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        UpdateGoldUI();
    }

    // 전투 보상, 이벤트, 상점 등 어디서 골드가 바뀌어도 자동으로 숫자를 맞춘다
    private void Update()
    {
        if (GoldManager.Instance != null && GoldManager.Instance.currentGold != shownGold)
            UpdateGoldUI();
    }

    public void UpdateGoldUI()
    {
        if (GoldManager.Instance == null || goldText == null) return;
        shownGold = GoldManager.Instance.currentGold;
        goldText.text = shownGold.ToString("N0");
    }

    // 몬스터 위치에서 상단 HUD로 아이콘이 날아가는 연출
    // addGold가 false면 연출만 하고 골드는 더하지 않는다 (실제 골드는 전투 보상에서 들어옴)
    public void PlayGoldFlyEffect(Vector3 startWorldPos, int goldAmount, bool addGold = true)
    {
        StartCoroutine(FlyEffectCoroutine(startWorldPos, goldAmount, addGold));
    }

    private IEnumerator FlyEffectCoroutine(Vector3 startWorldPos, int goldAmount, bool addGold)
    {
        if (floatingIconPrefab == null || canvasTransform == null || goldIconRect == null || Camera.main == null)
        {
            // 연출에 필요한 것이 없으면 즉시 반영
            if (addGold && GoldManager.Instance != null) GoldManager.Instance.AddGold(goldAmount);
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
        if (addGold && GoldManager.Instance != null) GoldManager.Instance.AddGold(goldAmount);
        UpdateGoldUI();

        // 5. 임시 오브젝트 파괴
        Destroy(iconObj);
    }
}