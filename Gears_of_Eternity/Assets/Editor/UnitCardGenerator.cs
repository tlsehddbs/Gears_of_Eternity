using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

using FactionTypes.Enums;
using RarityTypes.Enums;
using BattleTypes.Enums;

public class UnitCardGenerator : EditorWindow
{
    [MenuItem("Tools/Automatic Card Generator/Unit Cards")]
    public static void ShowWindow()
    {
        GetWindow<UnitCardGenerator>("Unit Card Generator");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("유닛 카드 생성 (CSV → ScriptableObject)"))
        {
            GenerateUnitCards();
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void GenerateUnitCards()
    {
        Dictionary<string, UnitCardData> unitMap = new();  // unitName → UnitCardData
        
        List<string[]> unitData = CSVLoader.LoadCSV("CSV/UnitCardData");
        List<string[]> upgradeData = CSVLoader.LoadCSV("CSV/UnitCardUpgradeData");

        if (unitData == null || unitData.Count == 0)
        {
            Debug.LogWarning("CSV 데이터 없음");
            return;
        }

        string outputFolder = "Assets/Resources/UnitCardAssets";

        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "UnitCardAssets");
        }

        // Generate Unit Assets
        foreach (var row in unitData)
        {
            UnitCardData card = ScriptableObject.CreateInstance<UnitCardData>();
            
            card.faction = (FactionType)Enum.Parse(typeof(FactionType), row[0]);
            card.unitName = row[1].Trim();
            // card.description = row[1];
            // card.unitPrefab = row[1];
            card.rarity = (Rarity)Enum.Parse(typeof(Rarity), row[2]);
            card.battleType = (BattleType)Enum.Parse(typeof(BattleType), row[3]);
            
            card.health = float.Parse(row[4]);
            card.defense = float.Parse(row[5]);
            
            card.moveSpeed = float.Parse(row[6]);

            card.attack = float.Parse(row[8]);
            card.attackSpeed = float.Parse(row[9]);
            card.attackRange = float.Parse(row[10]);        // 피해를 받는 범위
            card.attackDistance = float.Parse(row[11]);     // 공격 가능 범위
            
            card.cost = int.Parse(row[12]);

            card.level = int.Parse(row[13]);
            
            // skill 관련 parse 내용 작성
            
            string assetPath = $"{outputFolder}/{card.unitName}.asset";
            
            unitMap[card.unitName] = card;

            AssetDatabase.CreateAsset(card, assetPath);
            Debug.Log($"생성됨: {assetPath}");
        }
        Debug.Log("✅유닛 카드 생성 완료");
        
        // Generate Unit Upgrade Info(List)
        foreach (var row in upgradeData)
        {
            string baseUnit = row[1].Trim();
            string upgradeUnit = row[2].Trim();

            if (!unitMap.TryGetValue(baseUnit, out var baseCard))
            {
                Debug.LogWarning($"❗업그레이드 기준 유닛 '{baseUnit}' 없음");
                continue;
            }

            if (!unitMap.TryGetValue(upgradeUnit, out var upgradeCard))
            {
                Debug.LogWarning($"❗업그레이드 대상 유닛 '{upgradeUnit}' 없음");
                continue;
            }

            if (baseCard.nextUpgrades == null)
            {
                baseCard.nextUpgrades = new List<UnitCardData>();
            }

            baseCard.nextUpgrades.Add(upgradeCard);
            
            EditorUtility.SetDirty(baseCard);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}