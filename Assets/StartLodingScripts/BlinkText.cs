using UnityEngine;

public class BlinkText : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    public float blinkSpeed = 3f; // 깜빡이는 속도

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        // CanvasGroup이 없다면 자동으로 추가
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    void Update()
    {
        if (canvasGroup != null)
        {
            // 투명도(Alpha)를 0.2 ~ 1.0 사이로 빠르게/부드럽게 전환
            canvasGroup.alpha = 0.6f + Mathf.Sin(Time.time * blinkSpeed) * 0.4f;
        }
    }
}