using UnityEngine;
using UnityEngine.EventSystems;

public class CombineSlot : MonoBehaviour, IDropHandler
{
    [Header("반대편 조합 슬롯 참조")]
    public CombineSlot otherCombineSlot;

    public void OnDrop(PointerEventData eventData)
    {
        CardUI draggedCard = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<CardUI>() : null;

        if (draggedCard == null || draggedCard.cardData == null) return;

        // 1. 이미 내 슬롯에 카드가 들어있는 경우 거부
        if (GetComponentInChildren<CardUI>() != null)
        {
            Debug.Log("[조합 슬롯] 이미 카드가 올려져 있습니다.");
            return;
        }

        // 2. 반대편 슬롯에 이미 올려진 카드가 있는지 확인
        CardUI otherCard = otherCombineSlot != null ? otherCombineSlot.GetComponentInChildren<CardUI>() : null;

        if (otherCard != null && otherCard.cardData != null)
        {
            // 3. [핵심] 서로 다른 타입(공격-힐, 공격-디버프 등)이면 드롭을 거부함
            if (draggedCard.cardData.cardType != otherCard.cardData.cardType)
            {
                Debug.LogWarning($"[드롭 거부] 반대편 슬롯({otherCard.cardData.cardType})과 다른 타입({draggedCard.cardData.cardType})의 카드는 넣을 수 없습니다.");
                return; // parentToReturnTo를 변경하지 않고 리턴 -> 원래 손패(HandPanel)로 자동 복귀
            }
        }

        // 4. 타입이 같거나 반대편이 비어있으면 조합 슬롯 등록 허용
        draggedCard.parentToReturnTo = this.transform;
    }
}