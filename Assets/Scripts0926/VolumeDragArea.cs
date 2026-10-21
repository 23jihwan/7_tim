using System;
using UnityEngine;
using UnityEngine.EventSystems;

// 소리 막대를 마우스로 누르거나 끌면, 그 위치를 0~1 값으로 알려준다 (SettingsMenu가 자동으로 붙인다)
public class VolumeDragArea : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public Action<float> onValueChanged;

    public void OnPointerDown(PointerEventData eventData) { Report(eventData); }
    public void OnDrag(PointerEventData eventData) { Report(eventData); }

    private void Report(PointerEventData eventData)
    {
        RectTransform rt = (RectTransform)transform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out Vector2 local))
        {
            float value = Mathf.Clamp01((local.x - rt.rect.xMin) / rt.rect.width);
            onValueChanged?.Invoke(value);
        }
    }
}