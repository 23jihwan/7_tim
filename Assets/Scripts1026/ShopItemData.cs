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

#if UNITY_EDITOR
    // 에디터 우측 상단 점 3개 메뉴를 눌러서 데이터를 한 번에 가져오는 버튼
    [ContextMenu("카드/유물 데이터에서 이미지 및 정보 자동 가져오기")]
    public void SyncDataFromSource()
    {
        if (itemType == ShopItemType.Card && card != null)
        {
            itemName = card.cardName;
            itemDesc = card.description;
            itemIcon = card.cardIcon; // CardData의 이미지 변수명 연동

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[상점 데이터 동기화] 카드 ({card.cardName})의 정보와 이미지를 성공적으로 가져왔습니다!");
        }
        else if (itemType == ShopItemType.Relic && relic != null)
        {
            itemName = relic.relicName;
            itemDesc = relic.description;
            itemIcon = relic.icon; // RelicData의 이미지 변수명 연동

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[상점 데이터 동기화] 유물 ({relic.relicName})의 정보와 이미지를 성공적으로 가져왔습니다!");
        }
        else
        {
            Debug.LogWarning("연결된 카드나 유물 데이터가 없거나 아이템 타입이 올바르지 않습니다.");
        }
    }
#endif
}