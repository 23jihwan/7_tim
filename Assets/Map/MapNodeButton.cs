using UnityEngine;
using UnityEngine.UI;

// 노드의 상태
public enum NodeState
{
    Locked,     // 아직 갈 수 없는 앞쪽 방
    Reachable,  // 지금 갈 수 있는 방
    Current,    // 현재 위치
    Visited,    // 지나온 방
    Missed      // 지나쳐서 이제 갈 수 없는 방
}

// 맵 노드 프리팹에 붙이는 스크립트
// 구조: MapNode (Image + Button + 이 스크립트)
//         └ Icon (Image)
public class MapNodeButton : MonoBehaviour
{
    public Image background;   // 비워 두면 자기 자신의 Image를 자동으로 찾음
    public Image icon;         // 비워 두면 "Icon"이라는 자식을 자동으로 찾음
    public Button button;      // 비워 두면 자기 자신의 Button을 자동으로 찾음

    [Header("상태 표시")]
    public Color outlineColor = new Color32(0x1F, 0x14, 0x08, 0xFF);
    public float currentScale = 1.25f;

    public MapNode Node { get; private set; }

    private Color baseColor;
    private NodeState state;
    private Outline outline;

    private void Awake()
    {
        if (background == null) background = GetComponent<Image>();
        if (button == null) button = GetComponent<Button>();
        if (icon == null)
        {
            Transform child = transform.Find("Icon");
            if (child != null) icon = child.GetComponent<Image>();
        }

        // 버튼 기본 색 변화는 끄고, 상태 표시는 이 스크립트가 직접 한다
        if (button != null) button.transition = Selectable.Transition.None;

        outline = GetComponent<Outline>();
        if (outline == null) outline = gameObject.AddComponent<Outline>();
        outline.effectDistance = new Vector2(4, -4);
        outline.enabled = false;
    }

    public void Setup(MapNode node, Color color, Sprite iconSprite, float size)
    {
        Node = node;
        baseColor = color;

        RectTransform rt = (RectTransform)transform;
        rt.sizeDelta = new Vector2(size, size);
        rt.localRotation = Quaternion.Euler(0, 0, 45);   // 마름모 모양

        if (icon != null)
        {
            icon.rectTransform.localRotation = Quaternion.Euler(0, 0, -45);  // 아이콘은 똑바로
            icon.raycastTarget = false;
            icon.enabled = iconSprite != null;   // 아이콘 이미지가 없으면 색깔만 표시
            if (iconSprite != null) icon.sprite = iconSprite;
        }

        SetState(NodeState.Locked);
    }

    public void SetState(NodeState newState)
    {
        state = newState;

        Color color = baseColor;
        float scale = 1f;

        switch (state)
        {
            case NodeState.Visited:
                color = Color.Lerp(baseColor, Color.black, 0.35f);   // 조금 어둡게
                break;
            case NodeState.Missed:
                color.a = 0.3f;                                      // 흐리게
                break;
            case NodeState.Current:
                scale = currentScale;                                // 크게
                break;
        }

        if (background != null) background.color = color;
        if (icon != null) icon.color = new Color(1f, 1f, 1f, color.a);

        outline.enabled = state == NodeState.Reachable || state == NodeState.Current;
        outline.effectColor = outlineColor;

        if (button != null) button.interactable = state == NodeState.Reachable;
        transform.localScale = Vector3.one * scale;
    }

    private void Update()
    {
        // 갈 수 있는 방은 살짝 커졌다 작아졌다 하면서 눈에 띄게
        if (state == NodeState.Reachable)
        {
            float pulse = 1.1f + Mathf.Sin(Time.time * 4f) * 0.06f;
            transform.localScale = Vector3.one * pulse;
        }
    }
}