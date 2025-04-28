using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using FactionTypes.Enums;
using ItemEffectTypes.Enums;

public class ItemCardGenerator : EditorWindow
{
    [MenuItem("Tools/Automatic Card Generator/Item Cards")]
    public static void ShowWindow()
    {
        GetWindow<ItemCardGenerator>("Item Card Generator");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("아이템 카드 자동 생성 (CSV → ScriptableObject)"))
        {
            GenerateItemCards();
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void GenerateItemCards()
    {
        string csvPath = "CSV/ItemCardData";
        List<string[]> data = CSVLoader.LoadCSV(csvPath);

        if (data == null || data.Count == 0)
        {
            Debug.LogWarning("CSV 데이터 없음");
            return;
        }

        string outputFolder = "Assets/Resources/ItemCardAssets";

        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "UnitCardAssets");
        }

        foreach (var row in data)
        {
            // 유효성 검사
            if (row.Length < 6)
            {
                Debug.LogWarning("유효하지 않은 데이터(필드 부족)");
                continue;
            }
            
            ItemCardData card = ScriptableObject.CreateInstance<ItemCardData>();
            
            card.faction = (FactionType)Enum.Parse(typeof(FactionType), row[0]);
            card.itemName = row[1].Trim();
            //card.itemDescription = row[1];
            card.itemGroup = (ItemGroupType)Enum.Parse(typeof(ItemGroupType), row[2]);
            card.effectType = (ItemEffectType)Enum.Parse(typeof(ItemEffectType), row[6]);
            card.effectValue = float.Parse(row[7]);
            card.effectDuration = float.Parse(row[8]);
            
            card.itemTriggerCondition = (ItemTriggerConditionType)Enum.Parse(typeof(ItemTriggerConditionType), row[9]);
            card.itemTargetType = (ItemTargetType)Enum.Parse(typeof(ItemTargetType), row[10]);
            
            string assetPath = $"{outputFolder}/{card.itemName}.asset";

            AssetDatabase.CreateAsset(card, assetPath);
            Debug.Log($"생성됨: {assetPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅유닛 카드 생성 완료");
    }
}