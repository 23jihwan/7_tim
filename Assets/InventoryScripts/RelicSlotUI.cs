using UnityEngine;
using UnityEngine.UI;
using System;

public class RelicSlotUI : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private Image iconImage;          // Icon 자식 오브젝트의 Image 컴포넌트
    [SerializeField] private GameObject selectHighlight; // SelectHighlight 자식 오브젝트

    public RelicData Data { get; private set; }
    public int SlotIndex { get; private set; } = -1;

    private Action<RelicSlotUI> onClickAction;
    private Button button;

    private void Awake()
    {
        EnsureButtonSetup();
    }

    private void EnsureButtonSetup()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
            if (button == null)
            {
                // Button 컴포넌트가 없으면 자동으로 추가
                button = gameObject.AddComponent<Button>();
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnSlotClicked);
        }
    }

    public void Init(Action<RelicSlotUI> onClick, int slotIndex = -1)
    {
        EnsureButtonSetup(); // Init 호출 시점에 button이 null이면 즉시 구함
        this.onClickAction = onClick;
        this.SlotIndex = slotIndex;
    }

    public void SetData(RelicData relicData)
    {
        Data = relicData;

        if (relicData != null && relicData.icon != null)
        {
            if (iconImage != null)
            {
                iconImage.sprite = relicData.icon;
                iconImage.color = Color.white; // 불투명도 100% 보장
                iconImage.gameObject.SetActive(true); // 아이콘 오브젝트 활성화
            }
        }
        else
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.gameObject.SetActive(false); // 데이터가 없으면 비활성화
            }
        }
    }

    public void SetHighlight(bool isSelected)
    {
        if (selectHighlight != null)
        {
            selectHighlight.SetActive(isSelected);
        }
    }

    private void OnSlotClicked()
    {
        onClickAction?.Invoke(this);
    }
}