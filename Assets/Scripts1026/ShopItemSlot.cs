using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro를 사용하기 위해 필요합니다.

public class ShopItemSlot : MonoBehaviour
{
    [Header("Item Data")]
    public ShopItemData itemData; // 여기에 ShopItemData 에셋을 넣어줍니다.

    [Header("UI Component Links")]
    public Image iconImage;           // 슬롯 안의 아이템 이미지 컴포넌트
    public TextMeshProUGUI nameText;  // 슬롯 안의 아이템 이름 텍스트
    public TextMeshProUGUI priceText; // 슬롯 안의 가격 텍스트

    public bool IsSold { get; private set; }   // 이미 팔린 상품인지

    private void Start()
    {
        // 슬롯이 켜질 때 데이터가 있다면 자동으로 UI에 로드합니다.
        SetupSlot();

        // 버튼 컴포넌트가 있다면 클릭 이벤트를 자동으로 연결합니다.
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnSlotClick);
        }
    }

    // 에디터나 게임 시작 시 데이터를 화면에 뿌려주는 함수
    public void SetupSlot()
    {
        if (itemData != null)
        {
            // 1. 아이콘 이미지 로드
            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(true);
                iconImage.sprite = itemData.itemIcon;
            }

            // 2. 아이템 이름 로드 (New Text 글자가 데이터 이름으로 자동 바뀝니다!)
            if (nameText != null)
            {
                nameText.text = itemData.itemName;
            }

            // 3. 가격 로드
            if (priceText != null)
            {
                priceText.text = $"{itemData.itemPrice} 골드";
            }
        }
    }

    public void OnSlotClick()
    {
        if (IsSold) return;   // 팔린 상품은 눌러도 반응 없음

        // 씬에 있는 ShopManager를 찾아 선택된 아이템 데이터를 전달합니다.
        ShopManager shopManager = FindFirstObjectByType<ShopManager>();
        if (shopManager != null && itemData != null)
        {
            shopManager.OnClickItemSlot(itemData, this);   // 어느 칸인지도 함께 전달
        }
    }

    // 구매 성공 후 "판매 완료"로 바꾼다
    public void MarkSold()
    {
        IsSold = true;
        if (priceText != null) priceText.text = "판매 완료";
        if (iconImage != null) iconImage.color = new Color(1f, 1f, 1f, 0.35f);

        Button btn = GetComponent<Button>();
        if (btn != null) btn.interactable = false;
    }
}