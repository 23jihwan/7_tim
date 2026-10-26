using UnityEngine;

// 상점에서 파는 물건 종류
public enum ShopItemType { Card, Relic, Heal }

[CreateAssetMenu(fileName = "NewShopItem", menuName = "Shop/Item Data")]
public class ShopItemData : ScriptableObject
{
    public string itemName;      // 아이템 이름 (예: 하급 포션)
    [TextArea]
    public string itemDesc;      // 아이템 설명 (예: 체력을 50 회복시켜줍니다.)
    public int itemPrice;        // 가격 (예: 150)
    public Sprite itemIcon;      // 아이템 이미지 스프라이트

    [Header("사면 얻는 것")]
    public ShopItemType itemType = ShopItemType.Heal;
    [Tooltip("종류가 Card일 때: 덱에 들어갈 카드")]
    public CardData card;
    [Tooltip("종류가 Relic일 때: 얻을 유물")]
    public RelicData relic;
    [Tooltip("종류가 Heal일 때: 회복량")]
    public int healAmount = 30;
}