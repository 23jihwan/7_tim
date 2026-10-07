using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// 마우스를 놓은 위치에 겹쳐 있는 UI를 위에서부터 순서대로 Console에 출력한다 (테스트용)
public class DropDebugger : MonoBehaviour
{
    private void Update()
    {
        if (Mouse.current == null || EventSystem.current == null) return;
        if (!Mouse.current.leftButton.wasReleasedThisFrame) return;

        PointerEventData data = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, results);

        if (results.Count == 0)
        {
            Debug.Log("[드롭 확인] 마우스 아래에 UI가 없어요.");
            return;
        }

        List<string> names = new List<string>();
        foreach (RaycastResult r in results) names.Add(r.gameObject.name);
        Debug.Log("[드롭 확인] 위에서부터: " + string.Join("  →  ", names));
    }
}