using UnityEngine;
using UnityEngine.UI;

// 맵 로딩 화면: 탑의 문 앞에 서 있다가, 로딩이 진행되면 봉인(마법진)이 사라지고
// 문이 양쪽으로 열리며 안으로 걸어 들어가는 것처럼 화면이 다가간다
// 로딩 막대(Image의 Fill Amount 또는 Slider)를 연결하면 진행도에 맞춰 움직이고,
// 비워 두면 Duration 시간 동안 저절로 진행된다
public class DoorOpenEffect : MonoBehaviour
{
    [Header("움직일 것")]
    [Tooltip("배경과 문을 모두 담은 부모 (화면 전체가 문 쪽으로 다가간다)")]
    public RectTransform zoomRoot;
    public RectTransform leftDoor;     // Pivot X = 0 (왼쪽 경첩)
    public RectTransform rightDoor;    // Pivot X = 1 (오른쪽 경첩)
    [Tooltip("문 안쪽에서 퍼지는 빛 (선택)")]
    public Image doorGlow;
    [Tooltip("문 위의 마법진 봉인. 문이 열리기 전에 사라진다 (선택)")]
    public Graphic seal;
    [Tooltip("마지막에 화면을 금빛으로 덮는 이미지 (선택)")]
    public Image flash;

    [Header("진행도 (둘 중 하나, 비워 두면 시간으로 진행)")]
    public Image progressFill;
    public Slider progressSlider;
    public float duration = 3f;

    [Header("연출 값")]
    [Range(0f, 1f)] public float openStart = 0.45f;   // 진행도가 이 값을 넘으면 문이 열리기 시작
    public float zoomAmount = 0.35f;                  // 마지막에 얼마나 다가갈지
    public Vector2 zoomTarget = new Vector2(0, 15);   // 다가갈 지점 (문 가운데)

    private float timer;
    private float shown;      // 화면에 보이는 진행도 (부드럽게 따라간다)
    private bool doorSoundPlayed;

    private void Start()
    {
        Apply(0f);
    }

    private void Update()
    {
        timer += Time.unscaledDeltaTime;
        float target = ReadProgress();
        shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * 1.5f);
        Apply(shown);
    }

    private float ReadProgress()
    {
        if (progressFill != null) return progressFill.fillAmount;
        if (progressSlider != null) return Mathf.InverseLerp(progressSlider.minValue, progressSlider.maxValue, progressSlider.value);
        return Mathf.Clamp01(timer / Mathf.Max(0.1f, duration));
    }

    private void Apply(float p)
    {
        // 1. 봉인: 처음 절반 동안 서서히 사라진다
        float sealFade = Mathf.Clamp01(p / Mathf.Max(0.01f, openStart));
        if (seal != null)
        {
            Color c = seal.color; c.a = 1f - sealFade; seal.color = c;
            seal.rectTransform.localScale = Vector3.one * (1f + 0.25f * sealFade);
        }

        // 2. 문: openStart 이후 양쪽으로 열린다 (경첩 쪽으로 납작해지며 회전하는 느낌)
        float open = Smooth(Mathf.InverseLerp(openStart, 0.9f, p));
        if (open > 0.01f && !doorSoundPlayed) { doorSoundPlayed = true; SoundManager.Play("door"); }
        float sx = Mathf.Lerp(1f, 0.08f, open);
        if (leftDoor != null) leftDoor.localScale = new Vector3(sx, 1f, 1f);
        if (rightDoor != null) rightDoor.localScale = new Vector3(sx, 1f, 1f);

        // 3. 빛: 문이 열릴수록 강해진다
        if (doorGlow != null)
        {
            Color g = doorGlow.color; g.a = Mathf.Lerp(0.15f, 1f, open); doorGlow.color = g;
            doorGlow.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.6f, open);
        }

        // 4. 다가가기: 진행될수록 문 쪽으로 걸어 들어간다
        if (zoomRoot != null)
        {
            float z = 1f + zoomAmount * Smooth(p);
            zoomRoot.localScale = Vector3.one * z;
            zoomRoot.anchoredPosition = -zoomTarget * (z - 1f);
        }

        // 5. 마지막: 금빛으로 화면이 덮인다
        if (flash != null)
        {
            Color f = flash.color; f.a = Smooth(Mathf.InverseLerp(0.85f, 1f, p)); flash.color = f;
        }
    }

    private static float Smooth(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }
}
