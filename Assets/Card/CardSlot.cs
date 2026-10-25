using UnityEngine;
using UnityEngine.EventSystems;

public class CardSlot : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        CardUI card = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<CardUI>() : null;

        if (card != null)
        {
            // 슬롯당 카드는 1장만 등록 가능
            if (transform.childCount == 0)
            {
                card.parentToReturnTo = this.transform;
            }
        }
    }
}