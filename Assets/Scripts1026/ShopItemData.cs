using UnityEngine;

[CreateAssetMenu(fileName = "NewShopItem", menuName = "Shop/Item Data")]
public class ShopItemData : ScriptableObject
{
    public string itemName;      // 아이템 이름 (예: 하급 포션)
    [TextArea]
    public string itemDesc;      // 아이템 설명 (예: 체력을 50 회복시켜줍니다.)
    public int itemPrice;        // 가격 (예: 150)
    public Sprite itemIcon;      // 아이템 이미지 스프라이트
}