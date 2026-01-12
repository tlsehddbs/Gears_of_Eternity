using System;
using Unity.VisualScripting;
using UnityEngine;

public static class UnitUpgradePriceCalculator
{
    public const int Base12 = 40;
    public const int Base23 = 90;

    public const float R = 1.25f;   // 인플레이션
    public const float RestDiscount = 0.85f;

    public static int GetUpgradePrice(int currentLevel, int upgradeCount, StageTypes.StageNodeTypes type)
    {
        int basePrice = currentLevel switch
        {
            1 => Base12,
            2 => Base23,
            _ => int.MaxValue // 3이상은 업그레이드 불가
        };
        
        if (basePrice == int.MaxValue)
        {
            return int.MaxValue;
        }

        float inflation = Mathf.Pow(R, Mathf.Max(0, upgradeCount));
        float price = basePrice * inflation;

        if (type == StageTypes.StageNodeTypes.Rest)
        {
            price *= RestDiscount;
        }
        
        return Mathf.CeilToInt(price);
    }
}
