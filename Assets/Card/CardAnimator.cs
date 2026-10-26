using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 카드 애니메이션 모음 (뽑기, 놓기, 사용, 피격, 마우스 올리기)
// 손패와 슬롯은 Layout Group이 위치를 정하기 때문에, 진짜 카드를 직접 움직이지 않고
// "복사본(ghost)"을 날려 보낸 뒤 도착하면 진짜 카드를 보여주는 방식을 쓴다
public static class CardAnimator
{
    private static CardAnimRunner runner;
    private static readonly Dictionary<CardUI, float> hoverBaseY = new Dictionary<CardUI, float>();
    private static readonly Dictionary<CardUI, Coroutine> hoverRoutines = new Dictionary<CardUI, Coroutine>();

    // ---------------- 1. 카드 뽑기 ----------------
    // 화면 왼쪽 아래에서 손패 자리로 날아온다

    public static void PlayDraw(CardUI card, float delay)
    {
        if (card != null) Run(DrawRoutine(card, delay));
    }

    private static IEnumerator DrawRoutine(CardUI card, float delay)
    {
        CanvasGroup group = GroupOf(card);
        group.alpha = 0f;
        group.blocksRaycasts = false;

        yield return null;                                   // 손패 정렬이 끝날 때까지 한 프레임 기다림
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (card == null) yield break;
        Canvas.ForceUpdateCanvases();
        SoundManager.Play("card_draw");

        RectTransform root = RootCanvas(card);
        GameObject ghost = MakeGhost(card, root);
        RectTransform g = (RectTransform)ghost.transform;

        Vector3 start = root.TransformPoint(new Vector3(root.rect.xMin + 120f, root.rect.yMin - 160f, 0f));
        Vector3 end = card.transform.position;

        const float duration = 0.35f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (card == null) break;
            float k = EaseOut(t / duration);
            g.position = Vector3.Lerp(start, end, k);
            g.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, k);
            g.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-25f, 0f, k));
            yield return null;
        }

        Object.Destroy(ghost);
        if (card != null)
        {
            group.alpha = 1f;
            group.blocksRaycasts = true;
        }
    }

    // ---------------- 2. 놓기 / 손패로 돌아가기 ----------------
    // 놓은 자리에서 최종 자리로 부드럽게 미끄러지고, 슬롯에 들어가면 '탁' 튕긴다

    public static void PlaySettle(CardUI card, Vector3 fromWorld, bool punch)
    {
        if (card != null) Run(SettleRoutine(card, fromWorld, punch));
    }

    private static IEnumerator SettleRoutine(CardUI card, Vector3 fromWorld, bool punch)
    {
        SoundManager.Play(punch ? "card_place" : "card_hover");
        CanvasGroup group = GroupOf(card);
        group.alpha = 0f;

        yield return null;
        if (card == null) yield break;
        Canvas.ForceUpdateCanvases();

        RectTransform root = RootCanvas(card);
        GameObject ghost = MakeGhost(card, root);
        RectTransform g = (RectTransform)ghost.transform;
        Vector3 end = card.transform.position;
        g.position = fromWorld;

        const float duration = 0.18f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (card == null) break;
            g.position = Vector3.Lerp(fromWorld, end, EaseOut(t / duration));
            yield return null;
        }

        Object.Destroy(ghost);
        if (card == null) yield break;
        group.alpha = 1f;

        if (punch)
        {
            const float punchTime = 0.16f;
            for (float t = 0f; t < punchTime; t += Time.deltaTime)
            {
                if (card == null) yield break;
                float k = t / punchTime;
                card.transform.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            if (card != null) card.transform.localScale = Vector3.one;
        }
    }

    // ---------------- 3. 카드 사용 ----------------
    // 카드가 목표(몬스터 또는 플레이어 체력)로 날아가며 작아지고 사라진다
    // AttackManager에서 yield return StartCoroutine(CardAnimator.UseCard(...)) 로 쓴다

    public static IEnumerator UseCard(CardUI card, RectTransform target, Color glow)
    {
        if (card == null) yield break;
        CardHoverPreview.Hide(card);
        SoundManager.Play("card_use");

        RectTransform root = RootCanvas(card);
        GameObject ghost = MakeGhost(card, root);
        RectTransform g = (RectTransform)ghost.transform;
        CanvasGroup ghostGroup = ghost.GetComponent<CanvasGroup>();
        GroupOf(card).alpha = 0f;

        Outline outline = ghost.GetComponent<Outline>();
        if (outline == null) outline = ghost.AddComponent<Outline>();
        outline.effectColor = glow;
        outline.effectDistance = new Vector2(6f, -6f);

        Vector3 start = g.position;
        Vector3 end = target != null ? target.position : start + root.up * 300f * root.lossyScale.y;

        // 살짝 커지면서 빛나기
        const float charge = 0.12f;
        for (float t = 0f; t < charge; t += Time.deltaTime)
        {
            g.localScale = Vector3.one * Mathf.Lerp(1f, 1.15f, t / charge);
            yield return null;
        }

        // 목표로 날아가기
        const float fly = 0.25f;
        for (float t = 0f; t < fly; t += Time.deltaTime)
        {
            float k = EaseIn(t / fly);
            g.position = Vector3.Lerp(start, end, k);
            g.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.4f, k);
            g.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 20f, k));
            ghostGroup.alpha = Mathf.Lerp(1f, 0.3f, k);
            yield return null;
        }

        Object.Destroy(ghost);
    }

    // ---------------- 4. 피격 ----------------
    // 대상이 좌우로 흔들리고 색이 번쩍인다

    public static void Hit(RectTransform target, Color flash)
    {
        // 파란 빛이면 디버프, 나머지는 타격 소리
        SoundManager.Play(flash.b > flash.r ? "debuff" : "hit");
        if (target != null) Run(HitRoutine(target, flash));
    }

    private static IEnumerator HitRoutine(RectTransform target, Color flash)
    {
        Vector2 basePos = target.anchoredPosition;
        Graphic graphic = target.GetComponent<Graphic>();
        Color baseColor = graphic != null ? graphic.color : Color.white;

        const float duration = 0.35f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (target == null) yield break;
            float k = t / duration;
            float shake = Mathf.Sin(k * Mathf.PI * 8f) * 14f * (1f - k);
            target.anchoredPosition = basePos + new Vector2(shake, 0f);
            if (graphic != null) graphic.color = Color.Lerp(flash, baseColor, k);
            yield return null;
        }
        if (target == null) yield break;
        target.anchoredPosition = basePos;
        if (graphic != null) graphic.color = baseColor;
    }

    // 체력 글자 등을 잠깐 크게 했다가 돌려놓는다 (회복할 때)
    public static void Pulse(RectTransform target, Color flash)
    {
        SoundManager.Play("heal");
        if (target != null) Run(PulseRoutine(target, flash));
    }

    private static IEnumerator PulseRoutine(RectTransform target, Color flash)
    {
        Graphic graphic = target.GetComponent<Graphic>();
        Color baseColor = graphic != null ? graphic.color : Color.white;
        const float duration = 0.4f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (target == null) yield break;
            float k = t / duration;
            target.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(k * Mathf.PI));
            if (graphic != null) graphic.color = Color.Lerp(flash, baseColor, k);
            yield return null;
        }
        if (target == null) yield break;
        target.localScale = Vector3.one;
        if (graphic != null) graphic.color = baseColor;
    }

    // ---------------- 5. 마우스 올리면 떠오르기 (손패에서만) ----------------

    public static void SetHover(CardUI card, bool on)
    {
        if (card == null || card.transform.parent == null) return;
        if (card.transform.parent.GetComponent<HandPanelZone>() == null) return;   // 손패에 있을 때만

        RectTransform rt = (RectTransform)card.transform;
        if (on && !hoverBaseY.ContainsKey(card)) hoverBaseY[card] = rt.anchoredPosition.y;
        if (!hoverBaseY.TryGetValue(card, out float baseY)) return;

        float targetY = on ? baseY + 26f : baseY;
        if (on) SoundManager.Play("card_hover");
        if (!on) hoverBaseY.Remove(card);

        if (hoverRoutines.TryGetValue(card, out Coroutine running) && running != null) Runner.StopCoroutine(running);
        hoverRoutines[card] = Runner.StartCoroutine(RiseRoutine(rt, targetY));
    }

    // 카드를 잡기 시작할 때 떠오르기 상태를 지운다
    public static void ClearHover(CardUI card)
    {
        if (hoverRoutines.TryGetValue(card, out Coroutine running) && running != null) Runner.StopCoroutine(running);
        hoverRoutines.Remove(card);
        hoverBaseY.Remove(card);
    }

    private static IEnumerator RiseRoutine(RectTransform rt, float targetY)
    {
        const float duration = 0.12f;
        float startY = rt.anchoredPosition.y;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (rt == null) yield break;
            Vector2 p = rt.anchoredPosition;
            p.y = Mathf.Lerp(startY, targetY, EaseOut(t / duration));
            rt.anchoredPosition = p;
            yield return null;
        }
        if (rt == null) yield break;
        Vector2 end = rt.anchoredPosition;
        end.y = targetY;
        rt.anchoredPosition = end;
    }

    // ---------------- 도우미 ----------------

    private static CardAnimRunner Runner
    {
        get
        {
            if (runner == null)
            {
                GameObject go = new GameObject("CardAnimRunner");
                Object.DontDestroyOnLoad(go);
                runner = go.AddComponent<CardAnimRunner>();
            }
            return runner;
        }
    }

    private static void Run(IEnumerator routine)
    {
        Runner.StartCoroutine(routine);
    }

    private static CanvasGroup GroupOf(CardUI card)
    {
        CanvasGroup group = card.GetComponent<CanvasGroup>();
        if (group == null) group = card.gameObject.AddComponent<CanvasGroup>();
        return group;
    }

    private static RectTransform RootCanvas(CardUI card)
    {
        Canvas canvas = card.GetComponentInParent<Canvas>();
        return (RectTransform)canvas.rootCanvas.transform;
    }

    // 보기만 하는 카드 복사본 만들기
    private static GameObject MakeGhost(CardUI card, RectTransform root)
    {
        RectTransform src = (RectTransform)card.transform;
        GameObject ghost = Object.Instantiate(card.gameObject, root);
        ghost.name = "CardGhost";

        CardUI ui = ghost.GetComponent<CardUI>();
        if (ui != null) ui.enabled = false;
        CanvasGroup group = ghost.GetComponent<CanvasGroup>();
        if (group == null) group = ghost.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.blocksRaycasts = false;
        LayoutElement layout = ghost.GetComponent<LayoutElement>();
        if (layout != null) layout.ignoreLayout = true;

        RectTransform rt = (RectTransform)ghost.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = src.rect.size;
        rt.localScale = Vector3.one;
        rt.rotation = Quaternion.identity;
        rt.position = src.position;
        rt.SetAsLastSibling();
        return ghost;
    }

    private static float EaseOut(float x) { x = Mathf.Clamp01(x); return 1f - (1f - x) * (1f - x); }
    private static float EaseIn(float x) { x = Mathf.Clamp01(x); return x * x; }
}

// 코루틴을 대신 돌려 주는 빈 오브젝트 (자동으로 만들어진다)
public class CardAnimRunner : MonoBehaviour { }
