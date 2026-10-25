using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI playerGoldText;       // 상단 보유 골드 텍스트
    public GameObject notEnoughGoldPanel;        // "재화가 부족합니다" 팝업
    public GameObject itemDetailPopup;           // 아이템 상세 팝업 패널

    [Header("Popup UI References")]
    public Image popupItemIcon;                  // 팝업 내 아이템 이미지
    public TextMeshProUGUI popupItemNameText;    // 팝업 내 아이템 이름
    public TextMeshProUGUI popupItemDescText;    // 팝업 내 아이템 설명
    public TextMeshProUGUI popupItemPriceText;   // 팝업 내 아이템 가격

    private ShopItemData selectedItem;           // 현재 선택된 아이템 정보
    private float warningDisplayTime = 1.5f;
    private Coroutine warningCoroutine;

    private void Start()
    {
        UpdateShopGoldUI();
        if (notEnoughGoldPanel != null) notEnoughGoldPanel.SetActive(false);
        if (itemDetailPopup != null) itemDetailPopup.SetActive(false); // 시작할 때는 팝업 숨김
    }

    public void UpdateShopGoldUI()
    {
        if (playerGoldText != null && GoldManager.Instance != null)
        {
            playerGoldText.text = GoldManager.Instance.currentGold.ToString("N0");
        }
    }

    // 1. 상점 아이템 칸을 클릭했을 때 호출되는 함수 (인자로 아이템 데이터 전달)
    public void OnClickItemSlot(ShopItemData clickedItem)
    {
        selectedItem = clickedItem;

        // 팝업 UI에 데이터 반영
        if (popupItemIcon != null) popupItemIcon.sprite = clickedItem.itemIcon;
        if (popupItemNameText != null) popupItemNameText.text = clickedItem.itemName;
        if (popupItemDescText != null) popupItemDescText.text = clickedItem.itemDesc;
        if (popupItemPriceText != null) popupItemPriceText.text = $"{clickedItem.itemPrice} 골드";

        // 상세 팝업 켜기
        if (itemDetailPopup != null)
            itemDetailPopup.SetActive(true);
    }

    // 2. 팝업 창 안에서 [취소] 버튼을 눌렀을 때
    public void OnClickCancelPopup()
    {
        if (itemDetailPopup != null)
            itemDetailPopup.SetActive(false); // 팝업만 끄고 상점은 그대로 유지

        selectedItem = null;
    }

    // 3. 팝업 창 안에서 [구매] 버튼을 눌렀을 때
    public void OnClickConfirmBuy()
    {
        if (selectedItem == null) return;

        bool success = GoldManager.Instance.UseGold(selectedItem.itemPrice);

        if (success)
        {
            Debug.Log($"{selectedItem.itemName} 구매 성공!");
            UpdateShopGoldUI();

            // 구매 성공 시 팝업 닫기
            OnClickCancelPopup();

            // TODO: 플레이어 인벤토리에 아이템 지급 로직 추가
        }
        else
        {
            // 재화 부족 경고 띄우기
            ShowNotEnoughGoldWarning();
        }
    }

    private void ShowNotEnoughGoldWarning()
    {
        if (notEnoughGoldPanel == null) return;

        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
        }
        warningCoroutine = StartCoroutine(WarningRoutine());
    }

    private IEnumerator WarningRoutine()
    {
        notEnoughGoldPanel.SetActive(true);
        yield return new WaitForSeconds(warningDisplayTime);
        notEnoughGoldPanel.SetActive(false);
    }

    public void ReturnToBattleScene()
    {
        Debug.Log("전투 씬으로 이동합니다.");
        // UnityEngine.SceneManagement.SceneManager.LoadScene("BattleSceneName");
    }
}