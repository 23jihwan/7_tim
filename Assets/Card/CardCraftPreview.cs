using UnityEngine;

public class CardCraftPreview : MonoBehaviour
{
    [Header("카드 UI 및 패널 연결")]
    [Tooltip("미리보기 화면 전체를 감싸는 부모 오브젝트 (비활성화 시 통째로 숨겨짐)")]
    public GameObject previewContainer;

    [Tooltip("미리보기로 보여줄 자식 CardUI 컴포넌트")]
    public CardUI cardUI;

    [Header("도감 미해금 시 표시 설정")]
    public Sprite questionMarkSprite;         // 물음표 일러스트 이미지
    public string unknownCardName = "???";     // 미해금 카드 이름
    [TextArea]
    public string unknownDescription = "도감에 등록되지 않은 조합입니다."; // 미해금 설명

    private void Awake()
    {
        // 게임 시작 시 미리보기 UI 전체를 통째로 숨김
        HidePreview();
    }

    /// <summary>
    /// 조합 결과 카드의 미리보기를 화면에 표시합니다.
    /// </summary>
    public void ShowPreview(CardData resultCard)
    {
        if (resultCard == null)
        {
            HidePreview();
            return;
        }

        // 1. 숨겨져 있던 미리보기 패널 전체를 활성화
        if (previewContainer != null)
        {
            previewContainer.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }

        // 2. 도감 해금 여부 확인 (cardName 기준)
        bool isUnlocked = false;
        if (EncyclopediaManager.Instance != null)
        {
            isUnlocked = EncyclopediaManager.Instance.IsUnlocked(resultCard.cardName);
        }

        if (cardUI != null)
        {
            if (isUnlocked)
            {
                // 도감 해금 카드: 실제 결과 카드 데이터 연동
                cardUI.SetupCard(resultCard);
            }
            else
            {
                // 도감 미해금 카드: 물음표 카드 데이터 연동
                CardData lockedData = ScriptableObject.CreateInstance<CardData>();
                lockedData.cardName = unknownCardName;
                lockedData.cardIcon = questionMarkSprite;
                lockedData.description = unknownDescription;
                lockedData.cardType = resultCard.cardType;
                lockedData.attack = 0;
                lockedData.health = 0;

                cardUI.SetupCard(lockedData);
            }
        }
    }

    /// <summary>
    /// 미리보기 패널을 통째로 숨깁니다.
    /// </summary>
    public void HidePreview()
    {
        if (previewContainer != null)
        {
            previewContainer.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}