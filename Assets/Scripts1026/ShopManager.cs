using System.Collections;
using System.Collections.Generic;
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

    [Header("Shop Random Item Pools")]
    public List<ShopItemData> allTier3CardPool;  // 인스펙터에서 등록할 3단계 카드 전체 풀
    public List<ShopItemData> allRelicPool;      // 인스펙터에서 등록할 유물 전체 풀

    [Header("Shop Slots")]
    public ShopItemSlot[] shopSlots;             // 배치할 4개의 상점 슬롯 (인스펙터에서 4개 연결)

    private ShopItemData selectedItem;           // 현재 선택된 아이템 정보
    private ShopItemSlot selectedSlot;           // 현재 선택된 상품 칸 (판매 완료 표시용)
    private float warningDisplayTime = 1.5f;
    private Coroutine warningCoroutine;

    private void Start()
    {
        UpdateShopGoldUI();
        if (notEnoughGoldPanel != null) notEnoughGoldPanel.SetActive(false);
        if (itemDetailPopup != null) itemDetailPopup.SetActive(false); // 시작할 때는 팝업 숨김

        // ★ 상점에 들어올 때 [3단계 카드 2개 + 유물 2개] 랜덤 세팅 실행 (이미 산 아이템 제외)
        InitializeShopItems();
    }

    public void UpdateShopGoldUI()
    {
        if (playerGoldText != null && GoldManager.Instance != null)
        {
            playerGoldText.text = GoldManager.Instance.currentGold.ToString("N0");
        }
    }

    // 상점에 진입할 때 풀에서 랜덤으로 아이템을 뽑아 슬롯에 배정 (이미 산 것 제외)
    private void InitializeShopItems()
    {
        if (shopSlots == null || shopSlots.Length < 4)
        {
            Debug.LogWarning("상점 슬롯(shopSlots)이 4개 미만으로 설정되어 있습니다!");
            return;
        }

        RunState run = RunState.GetOrCreate();

        // 1. 전체 풀에서 "이미 구매한 아이템"을 필터링한 가상의 임시 풀 생성
        List<ShopItemData> availableCards = GetFilteredPool(allTier3CardPool, run);
        List<ShopItemData> availableRelics = GetFilteredPool(allRelicPool, run);

        // 2. 필터링된 남은 아이템들 중에서 각각 무작위 2개씩 선택 (상점 내 중복 방지 포함)
        List<ShopItemData> selectedCards = GetRandomItemsFromPool(availableCards, 2);
        List<ShopItemData> selectedRelics = GetRandomItemsFromPool(availableRelics, 2);

        // 3. 총 4개 아이템 조합 (앞 2개: 카드, 뒤 2개: 유물)
        List<ShopItemData> displayItems = new List<ShopItemData>();
        displayItems.AddRange(selectedCards);
        displayItems.AddRange(selectedRelics);

        // 4. 각 슬롯에 데이터 주입 및 활성화
        for (int i = 0; i < shopSlots.Length; i++)
        {
            if (i < displayItems.Count && displayItems[i] != null)
            {
                shopSlots[i].itemData = displayItems[i]; // 슬롯의 데이터 갱신
                shopSlots[i].SetupSlot();                // 슬롯 UI 새로고침
                shopSlots[i].gameObject.SetActive(true);
            }
            else
            {
                // 아이템이 부족할 경우 슬롯 숨김 처리
                shopSlots[i].gameObject.SetActive(false);
            }
        }
    }

    // 이미 산 아이템은 풀에서 빼버리는 필터링 함수
    private List<ShopItemData> GetFilteredPool(List<ShopItemData> originalPool, RunState run)
    {
        List<ShopItemData> filtered = new List<ShopItemData>();
        if (originalPool == null) return filtered;

        foreach (var item in originalPool)
        {
            if (item != null && !run.IsItemPurchased(item))
            {
                filtered.Add(item); // 아직 안 산 아이템만 후보에 남김
            }
        }
        return filtered;
    }

    // 리스트에서 중복 없이 무작위로 N개를 뽑는 헬퍼 함수
    private List<ShopItemData> GetRandomItemsFromPool(List<ShopItemData> pool, int count)
    {
        List<ShopItemData> result = new List<ShopItemData>();
        if (pool == null || pool.Count == 0) return result;

        List<ShopItemData> tempPool = new List<ShopItemData>(pool);
        int pickCount = Mathf.Min(count, tempPool.Count);

        for (int i = 0; i < pickCount; i++)
        {
            int randIndex = Random.Range(0, tempPool.Count);
            result.Add(tempPool[randIndex]);
            tempPool.RemoveAt(randIndex); // 이번 상점 내 중복 뽑기 방지
        }

        return result;
    }

    // 1. 상점 아이템 칸을 클릭했을 때 호출되는 함수
    public void OnClickItemSlot(ShopItemData clickedItem, ShopItemSlot slot = null)
    {
        selectedItem = clickedItem;
        selectedSlot = slot;
        SoundManager.Play("page");

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
            itemDetailPopup.SetActive(false);

        selectedItem = null;
        selectedSlot = null;
    }

    // 3. 팝업 창 안에서 [구매] 버튼을 눌렀을 때
    public void OnClickConfirmBuy()
    {
        if (selectedItem == null) return;

        if (!CanReceive(selectedItem)) { SoundManager.Play("error"); return; }

        bool success = GoldManager.Instance.UseGold(selectedItem.itemPrice);

        if (success)
        {
            Debug.Log($"{selectedItem.itemName} 구매 성공!");
            SoundManager.Play("coin");
            GiveItem(selectedItem);
            if (selectedSlot != null) selectedSlot.MarkSold();
            UpdateShopGoldUI();

            OnClickCancelPopup();
        }
        else
        {
            SoundManager.Play("error");
            ShowNotEnoughGoldWarning();
        }
    }

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

    private void GiveItem(ShopItemData item)
    {
        RunState run = RunState.GetOrCreate();

        // ★ 이 아이템을 영구 구매 목록에 등록 (다시는 상점에 안 뜸)
        run.AddPurchasedItem(item);

        switch (item.itemType)
        {
            case ShopItemType.Card:
                run.AddCard(item.card);

                if (item.card != null && EncyclopediaManager.Instance != null)
                {
                    EncyclopediaManager.Instance.UnlockCard(item.card.cardName);
                    Debug.Log($"[상점 구매] {item.card.cardName} 카드가 도감 및 조합 가능 데이터에 해금되었습니다.");
                }
                break;

            case ShopItemType.Relic:
                run.AddRelic(item.relic);
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

    public void ReturnToBattleScene()
    {
        string mapScene = RunState.Instance != null ? RunState.Instance.mapSceneName : "minhyeokMapTestScene";
        Debug.Log("상점을 나가 맵으로 돌아갑니다.");
        SceneManager.LoadScene(mapScene);
    }
}