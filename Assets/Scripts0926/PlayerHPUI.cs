using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem; // ★ 신규 Input System 네임스페이스 추가

public class PlayerHPUI : MonoBehaviour
{
    [Header("UI 컴포넌트 연결")]
    public Image hpFillImage;  // HP_Fill
    public TMP_Text hpText;    // HP_Text

    [Header("체력 설정")]
    public float maxHP = 100f;
    public float currentHP = 100f;

    [Header("애니메이션 설정")]
    public float lerpSpeed = 5f;

    private float targetFillAmount = 1f;

    private void Awake()
    {
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        targetFillAmount = maxHP > 0 ? currentHP / maxHP : 0;

        if (hpFillImage != null)
        {
            hpFillImage.fillAmount = targetFillAmount;
        }

        UpdateTextUI();
    }

    private void Update()
    {
        
        if (hpFillImage != null)
        {
            hpFillImage.fillAmount = Mathf.Lerp(hpFillImage.fillAmount, targetFillAmount, Time.deltaTime * lerpSpeed);
        }
    }

    public void SetHP(float newHP, float newMaxHP)
    {
        maxHP = Mathf.Max(1f, newMaxHP);
        currentHP = Mathf.Clamp(newHP, 0, maxHP);
        targetFillAmount = currentHP / maxHP;
        UpdateTextUI();
    }

    public void SetHP(float newHP)
    {
        SetHP(newHP, maxHP);
    }

    public void TakeDamage(float damage)
    {
        SetHP(currentHP - damage);
    }

    public void Heal(float amount)
    {
        SetHP(currentHP + amount);
    }

    private void UpdateTextUI()
    {
        if (hpText != null)
        {
            hpText.text = $"HP: {Mathf.RoundToInt(currentHP)}";
        }
    }
}