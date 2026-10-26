using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
    private ShopItemSlot selectedSlot;           // 현재 선택된 상품 칸 (판매 완료 표시용)
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
    public void OnClickItemSlot(ShopItemData clickedItem, ShopItemSlot slot = null)
    {
        selectedItem = clickedItem;
        selectedSlot = slot;

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
        selectedSlot = null;
    }

    // 3. 팝업 창 안에서 [구매] 버튼을 눌렀을 때
    public void OnClickConfirmBuy()
    {
        if (selectedItem == null) return;

        // 살 수 없는 경우는 골드를 내기 전에 먼저 막는다
        if (!CanReceive(selectedItem)) return;

        bool success = GoldManager.Instance.UseGold(selectedItem.itemPrice);

        if (success)
        {
            Debug.Log($"{selectedItem.itemName} 구매 성공!");
            GiveItem(selectedItem);                               // 산 물건 지급 (덱, 유물, 체력)
            if (selectedSlot != null) selectedSlot.MarkSold();    // 판매 완료 표시
            UpdateShopGoldUI();

            // 구매 성공 시 팝업 닫기
            OnClickCancelPopup();
        }
        else
        {
            // 재화 부족 경고 띄우기
            ShowNotEnoughGoldWarning();
        }
    }

    // 받을 수 있는 물건인지 확인 (이미 가진 유물, 체력이 가득 찬 상태의 회복)
    private bool CanReceive(ShopItemData item)
    {
        RunState run = RunState.GetOrCreate();

        if (item.itemType == ShopItemType.Relic && item.relic != null && run.ownedRelics.Contains(item.relic))
        {
            Debug.Log("이미 가지고 있는 유물이에요.");
            return false;
        }
        if (item.itemType == ShopItemType.Heal && run.playerHP >= run.playerMaxHP)
        {
            Debug.Log("체력이 이미 가득 차 있어요.");
            return false;
        }
        return true;
    }

    // 종류에 따라 산 물건을 RunState에 넣는다 (씬이 바뀌어도 유지)
    private void GiveItem(ShopItemData item)
    {
        RunState run = RunState.GetOrCreate();

        switch (item.itemType)
        {
            case ShopItemType.Card:
                run.AddCard(item.card);       // 덱에 추가 + 도감 등록
                break;

            case ShopItemType.Relic:
                run.AddRelic(item.relic);     // 유물 획득 (인벤토리에서 장착)
                break;

            case ShopItemType.Heal:
                int before = run.playerHP;
                run.playerHP = Mathf.Min(run.playerMaxHP, run.playerHP + item.healAmount);
                Debug.Log($"체력 회복: {before} → {run.playerHP}");
                break;
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

    // 상점 나가기: 맵으로 돌아간다 (버튼에 연결된 이름은 그대로 둠)
    public void ReturnToBattleScene()
    {
        string mapScene = RunState.Instance != null ? RunState.Instance.mapSceneName : "minhyeokMapTestScene";
        Debug.Log("상점을 나가 맵으로 돌아갑니다.");
        SceneManager.LoadScene(mapScene);
    }
}