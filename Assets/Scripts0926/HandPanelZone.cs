using UnityEngine;
using UnityEngine.EventSystems;

public class HandPanelZone : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        CardUI card = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<CardUI>() : null;

        if (card != null)
        {
            card.parentToReturnTo = this.transform;
        }
    }
}