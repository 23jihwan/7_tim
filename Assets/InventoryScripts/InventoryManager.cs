using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    [Header("UI - Top Equip Slots (5개)")]
    [SerializeField] private RelicSlotUI[] equipSlots = new RelicSlotUI[5];

    [Header("UI - Detail Panel")]
    [SerializeField] private Image detailIcon;
    [SerializeField] private TextMeshProUGUI detailNameText;
    [SerializeField] private TextMeshProUGUI detailDescText;

    [Header("UI - Detail Panel Buttons")]
    [SerializeField] private Button equipButton;
    [SerializeField] private Button swapButton;

    [Header("UI - Scroll View")]
    [SerializeField] private RectTransform scrollContent;
    [SerializeField] private GameObject relicSlotPrefab;

    [Header("Data List")]
    [Tooltip("테스트용 시작 유물. 런을 시작할 때 RunState에 한 번 넣어 준다")]
    [SerializeField] private List<RelicData> ownedRelics = new List<RelicData>();
    [SerializeField] private bool giveAsStartingRelics = true;

    // 내부 상태 관리
    private RelicData selectedRelic;               // 현재 선택된 유물
    private int selectedEquipSlotIndex = -1;       // 클릭된 상단 장착 슬롯 번호
    private bool isEquipSlotSelected = false;      // 선택된 대상이 상단 슬롯인지 여부
    private RelicData[] equippedRelics = new RelicData[5]; // 장착 중인 유물 5개

    private List<RelicSlotUI> spawnedScrollSlots = new List<RelicSlotUI>();

    private void Start()
    {
        SyncWithRunState();
        InitEquipSlots();
        InitButtons();
        RefreshScrollList();
        UpdateDetailPanel(null);
    }

    // ★ 보유·장착 유물을 씬이 바뀌어도 유지되는 RunState의 목록과 연결한다
    private void SyncWithRunState()
    {
        RunState run = RunState.GetOrCreate();

        // Inspector에 넣어 둔 유물을 시작 유물로 한 번만 넣어 준다 (테스트용)
        if (giveAsStartingRelics && !run.startingRelicsGiven)
        {
            foreach (RelicData relic in ownedRelics) run.AddRelic(relic);
            run.startingRelicsGiven = true;
        }

        // 같은 목록을 함께 쓰므로, 여기서 장착하면 RunState에도 바로 기록된다
        ownedRelics = run.ownedRelics;
        equippedRelics = run.equippedRelics;
    }

    private void InitEquipSlots()
    {
        for (int i = 0; i < equipSlots.Length; i++)
        {
            if (equipSlots[i] == null) continue;

            int index = i;
            equipSlots[i].Init(OnEquipSlotClicked, index);
            equipSlots[i].SetData(i < equippedRelics.Length ? equippedRelics[i] : null);   // ★ 장착 중인 유물을 다시 보여준다
            equipSlots[i].SetHighlight(false);
        }
    }

    private void InitButtons()
    {
        if (equipButton != null)
        {
            equipButton.onClick.RemoveAllListeners();
            equipButton.onClick.AddListener(OnEquipButtonClicked);
        }

        if (swapButton != null)
        {
            swapButton.onClick.RemoveAllListeners();
            swapButton.onClick.AddListener(OnSwapButtonClicked);
        }
    }

    // 장착되지 않은 유물만 아래 스크롤 목록에 표시
    public void RefreshScrollList()
    {
        SyncWithRunState();   // ★ 이벤트 등에서 새로 얻은 유물도 보이게

        foreach (var slot in spawnedScrollSlots)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        spawnedScrollSlots.Clear();

        if (relicSlotPrefab == null || scrollContent == null) return;

        for (int i = 0; i < ownedRelics.Count; i++)
        {
            RelicData data = ownedRelics[i];

            // 이미 장착된 유물이면 스크롤 목록 생성에서 제외 (인벤토리에서 숨김)
            if (IsEquipped(data)) continue;

            GameObject obj = Instantiate(relicSlotPrefab, scrollContent);
            RelicSlotUI slotUI = obj.GetComponent<RelicSlotUI>();
            if (slotUI != null)
            {
                slotUI.Init(OnScrollSlotClicked);
                slotUI.SetData(data);
                slotUI.SetHighlight(false);
                spawnedScrollSlots.Add(slotUI);
            }
        }
    }

    private bool IsEquipped(RelicData data)
    {
        if (data == null) return false;
        for (int i = 0; i < equippedRelics.Length; i++)
        {
            if (equippedRelics[i] == data) return true;
        }
        return false;
    }

    // 아래 스크롤 목록 유물 클릭 시
    private void OnScrollSlotClicked(RelicSlotUI clickedSlot)
    {
        selectedRelic = clickedSlot.Data;
        isEquipSlotSelected = false;

        // 하이라이트 해제 및 적용
        ClearAllHighlights();
        clickedSlot.SetHighlight(true);

        UpdateDetailPanel(selectedRelic);
    }

    // 상단 장착 슬롯 클릭 시
    private void OnEquipSlotClicked(RelicSlotUI clickedSlot)
    {
        selectedEquipSlotIndex = clickedSlot.SlotIndex;
        isEquipSlotSelected = true;

        ClearAllHighlights();
        clickedSlot.SetHighlight(true);

        if (selectedEquipSlotIndex >= 0 && selectedEquipSlotIndex < equippedRelics.Length)
        {
            selectedRelic = equippedRelics[selectedEquipSlotIndex];
            UpdateDetailPanel(selectedRelic);
        }
    }

    private void ClearAllHighlights()
    {
        foreach (var slot in spawnedScrollSlots)
        {
            if (slot != null) slot.SetHighlight(false);
        }
        for (int i = 0; i < equipSlots.Length; i++)
        {
            if (equipSlots[i] != null) equipSlots[i].SetHighlight(false);
        }
    }

    // '장착' 버튼 클릭 시 (인벤토리 유물 -> 상단 빈 슬롯)
    private void OnEquipButtonClicked()
    {
        if (selectedRelic == null || isEquipSlotSelected) return;

        // 빈 슬롯 찾기
        for (int i = 0; i < equippedRelics.Length; i++)
        {
            if (equippedRelics[i] == null)
            {
                equippedRelics[i] = selectedRelic;
                equipSlots[i].SetData(selectedRelic); // 상단 슬롯에 이미지 표시
                break;
            }
        }

        selectedRelic = null;
        RefreshScrollList(); // 목록 재갱신 (장착된 유물 숨김)
        UpdateDetailPanel(null);
    }

    // '교체' 버튼 클릭 시 (선택한 인벤토리 유물 <-> 선택된 상단 슬롯)
    private void OnSwapButtonClicked()
    {
        if (selectedRelic == null || selectedEquipSlotIndex < 0) return;

        // 교체되어 해제되는 기존 유물은 자동으로 스크롤 목록에 다시 나타남
        equippedRelics[selectedEquipSlotIndex] = selectedRelic;
        equipSlots[selectedEquipSlotIndex].SetData(selectedRelic);

        selectedRelic = null;
        selectedEquipSlotIndex = -1;

        RefreshScrollList(); // 목록 재갱신
        UpdateDetailPanel(null);
    }

    // 상세 창 UI 업데이트
    private void UpdateDetailPanel(RelicData data)
    {
        if (data == null)
        {
            if (detailIcon != null) detailIcon.gameObject.SetActive(false);
            if (detailNameText != null) detailNameText.text = "유물을 선택하세요.";
            if (detailDescText != null) detailDescText.text = "";
            if (equipButton != null) equipButton.interactable = false;
            if (swapButton != null) swapButton.interactable = false;
            return;
        }

        if (detailIcon != null)
        {
            detailIcon.sprite = data.icon;
            detailIcon.gameObject.SetActive(true);
        }

        if (detailNameText != null) detailNameText.text = data.relicName;
        if (detailDescText != null) detailDescText.text = data.description;

        // 인벤토리 유물 선택 시 '장착' 가능 / 상단 장착 슬롯 선택 시 '교체' 가능
        if (equipButton != null) equipButton.interactable = !isEquipSlotSelected && !IsEquipped(data);
        if (swapButton != null) swapButton.interactable = !isEquipSlotSelected && selectedEquipSlotIndex >= 0;
    }

    private void OnEnable()
    {
        // 팝업 패널이 켜질 때마다 스크롤 목록 및 슬롯 초기화 실행
        SyncWithRunState();
        InitEquipSlots();
        InitButtons();
        RefreshScrollList();
        UpdateDetailPanel(null);
    }
}