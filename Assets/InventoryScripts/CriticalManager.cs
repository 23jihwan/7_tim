using System.Collections.Generic;
using UnityEngine;

public class CriticalManager : MonoBehaviour
{
    public static CriticalManager Instance { get; private set; }

    [Header("치명타 설정")]
    private const float MAX_CRIT_CHANCE = 100f; // 최대 치명타 확률 100% 제한

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
    /// 현재 장착 중인 5개 유물 슬롯 리스트를 받아 총 치명타 확률 계산
    /// </summary>
    public float GetTotalCriticalChance(List<RelicData> equippedRelics)
    {
        float totalChance = 0f;

        if (equippedRelics == null) return 0f;

        foreach (var relic in equippedRelics)
        {
            if (relic != null && relic.statBonusType == StatBonusType.CriticalChanceBonus)
            {
                totalChance += relic.value;
            }
        }

        // 최대 100%로 제한
        return Mathf.Clamp(totalChance, 0f, MAX_CRIT_CHANCE);
    }

    /// <summary>
    /// 최종 데미지 계산 (치명타 성공 시 기본 공격력의 2배 적용)
    /// </summary>
    /// <param name="baseDamage">기본 공격력</param>
    /// <param name="equippedRelics">현재 장착된 유물 리스트</param>
    /// <param name="isCritical">치명타 발동 여부 반환</param>
    public int CalculateDamage(int baseDamage, List<RelicData> equippedRelics, out bool isCritical)
    {
        float critChance = GetTotalCriticalChance(equippedRelics);
        float randomValue = Random.Range(0f, 100f);

        if (randomValue < critChance)
        {
            isCritical = true;
            return baseDamage * 2; // 치명타 시 기본 공격력의 2배
        }

        isCritical = false;
        return baseDamage;
    }
}