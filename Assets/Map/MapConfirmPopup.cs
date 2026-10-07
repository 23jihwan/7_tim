using System;
using UnityEngine;
using UnityEngine.UI;

// 노드를 눌렀을 때 "정말 들어갈까요?"를 묻는 확인창
// 구조: ConfirmPopup (Panel, 이 스크립트)
//         └ Window (Image)
//             ├ Title (Text)
//             ├ Description (Text)
//             ├ CancelButton (Button)
//             └ ConfirmButton (Button)
public class MapConfirmPopup : MonoBehaviour
{
    public Text titleText;
    public Text descriptionText;
    public Button confirmButton;
    public Button cancelButton;

    private Action onConfirm;
    private bool initialized;

    // 처음 열릴 때 한 번만 버튼을 연결한다
    private void Init()
    {
        if (initialized) return;
        initialized = true;

        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(Hide);
    }

    public void Show(string title, string description, Action confirmAction)
    {
        Init();
        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = description;
        onConfirm = confirmAction;

        transform.SetAsLastSibling();   // 항상 맨 위에 보이게
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        onConfirm = null;
        gameObject.SetActive(false);
    }

    private void Confirm()
    {
        Action action = onConfirm;
        Hide();
        action?.Invoke();
    }
}