using System.Collections.Generic;
using UnityEngine;

public class CriticalManager : MonoBehaviour
{
    public static CriticalManager Instance { get; private set; }

    private const float MAX_CRIT_CHANCE = 100f; // 최대 치명타 확률 100%

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 장착 중인 유물 목록(배열 또는 리스트)에서 총 치명타 확률 합산 (최대 100% 제한)
    /// </summary>
    public float GetTotalCriticalChance(IEnumerable<RelicData> equippedRelics)
    {
        if (equippedRelics == null)
        {
            Debug.LogWarning("[CriticalManager] 장착된 유물 목록이 null입니다.");
            return 0f;
        }

        float totalChance = 0f;

        foreach (var relic in equippedRelics)
        {
            if (relic != null && relic.statBonusType == StatBonusType.CriticalChanceBonus)
            {
                totalChance += relic.value;
            }
        }

        return Mathf.Clamp(totalChance, 0f, MAX_CRIT_CHANCE);
    }

    /// <summary>
    /// 최종 데미지 연산 (치명타 100% 만족 시 baseDamage * 2 적용)
    /// </summary>
    public int CalculateDamage(int baseDamage, IEnumerable<RelicData> equippedRelics, out bool isCritical)
    {
        float critChance = GetTotalCriticalChance(equippedRelics);
        float randomValue = Random.Range(0f, 100f);

        // critChance가 100%이면 randomValue < 100f는 무조건 true
        if (randomValue < critChance)
        {
            isCritical = true;
            int doubleDamage = baseDamage * 2;
            Debug.Log($"[CriticalManager] ★ 치명타 발동! (확률: {critChance}%) | {baseDamage} -> {doubleDamage} 데미지");
            return doubleDamage;
        }

        isCritical = false;
        Debug.Log($"[CriticalManager] 일반 공격 (확률: {critChance}%) | 데미지: {baseDamage}");
        return baseDamage;
    }
}