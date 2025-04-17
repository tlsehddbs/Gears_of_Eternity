using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using ItemEffectTypes.Enums;

public class ItemCardGenerator : EditorWindow
{
    [MenuItem("Tools/Card Generator/Generate Item Cards")]
    public static void ShowWindow()
    {
        GetWindow<ItemCardGenerator>("Item Card Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Item Card CSV 생성기", EditorStyles.boldLabel);
        if (GUILayout.Button("💾 아이템 카드 자동 생성 (CSV → SO)"))
        {
            GenerateItemCards();
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void GenerateItemCards()
    {
        string csvPath = "CSV/ItemCardData"; // Resources/CSV/aa.csv
        List<string[]> data = CSVLoader.LoadCSV(csvPath);

        if (data == null || data.Count == 0)
        {
            Debug.LogWarning("CSV 데이터가 없습니다.");
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
                Debug.LogWarning("유효하지 않은 데이터 행 (필드 부족)");
                continue;
            }

            ItemCardData card = ScriptableObject.CreateInstance<ItemCardData>();
            card.itemName = row[0];
            card.itemDescription = row[1];
            card.effectType = (ItemEffectType)Enum.Parse(typeof(ItemEffectType), row[2]);
            card.effectValue = float.Parse(row[3]);
            card.effectDuration = float.Parse(row[4]);
            
            string assetPath = $"{outputFolder}/{card.itemName}.asset";

            AssetDatabase.CreateAsset(card, assetPath);
            Debug.Log($"생성됨: {assetPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ 유닛 카드 생성 완료!");
    }
}