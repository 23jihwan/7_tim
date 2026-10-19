using UnityEngine;

public class InventoryToggle : MonoBehaviour
{
    [Header("인벤토리 팝업 패널")]
    [SerializeField] private GameObject inventoryPanel; // MainPanel 연결

    // 인벤토리 창 열기/닫기 토글 (버튼 클릭 시)
    public void ToggleInventory()
    {
        if (inventoryPanel != null)
        {
            bool isActive = inventoryPanel.activeSelf;
            inventoryPanel.SetActive(!isActive);

            if (!isActive)
            {
                RefreshInventoryUI();
            }
        }
    }

    // 인벤토리 열기 전용
    public void OpenInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            RefreshInventoryUI();
        }
    }

    // 인벤토리 닫기 전용 ('X' 버튼에 연결)
    public void CloseInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
    }

    private void RefreshInventoryUI()
    {
        InventoryManager manager = inventoryPanel.GetComponentInChildren<InventoryManager>(true);
        if (manager != null)
        {
            manager.RefreshScrollList();
        }
    }
}