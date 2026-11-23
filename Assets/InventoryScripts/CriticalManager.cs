using System.Collections.Generic;
using UnityEngine;

public class CriticalManager : MonoBehaviour
{
    public static CriticalManager Instance { get; private set; }
    private const float MAX_CRIT_CHANCE = 100f;

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

    public float GetTotalCriticalChance(IEnumerable<RelicData> equippedRelics)
    {
        if (equippedRelics == null) return 0f;

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

    public int CalculateDamage(int baseDamage, IEnumerable<RelicData> equippedRelics, out bool isCritical)
    {
        float critChance = GetTotalCriticalChance(equippedRelics);
        float randomValue = Random.Range(0f, 100f);

        if (randomValue < critChance)
        {
            isCritical = true;
            int doubleDamage = baseDamage * 2;
            Debug.Log($"[CriticalManager] ★ 치명타 발동! (확률: {critChance}%) | {baseDamage} -> {doubleDamage}");
            return doubleDamage;
        }

        isCritical = false;
        return baseDamage;
    }
}