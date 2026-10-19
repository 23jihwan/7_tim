using UnityEngine;
using UnityEngine.EventSystems;

public class CombineSlot : MonoBehaviour, IDropHandler
{
    [Header("반대편 조합 슬롯 참조 (선택)")]
    public CombineSlot otherCombineSlot;

    public void OnDrop(PointerEventData eventData)
    {
        CardUI draggedCard = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<CardUI>() : null;

        if (draggedCard == null || draggedCard.cardData == null) return;

        // 이미 카드가 있는 경우에만 드롭 거부
        if (GetComponentInChildren<CardUI>() != null)
        {
            Debug.Log("[조합 슬롯] 이미 카드가 올려져 있습니다.");
            return;
        }

        // 제한 없이 드롭 허용
        draggedCard.parentToReturnTo = this.transform;
    }
}