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

        if (GetComponentInChildren<CardUI>() != null)
        {
            Debug.Log("[조합 슬롯] 이미 카드가 올려져 있습니다.");
            return;
        }

        draggedCard.parentToReturnTo = this.transform;
    }
}