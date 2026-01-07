using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using FactionTypes.Enums;
using ItemEffectTypes.Enums;

public class ItemCardScriptableObjectGenerator : EditorWindow
{
    [MenuItem("Tools/Automatic Card Generator/Item Cards")]
    public static void ShowWindow()
    {
        GetWindow<ItemCardScriptableObjectGenerator>("Item Card Generator");
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
        string csvPath = "CSV/ItemData";
        List<string[]> data = CSVLoader.LoadCSV(csvPath);

        if (data == null || data.Count == 0)
        {
            Debug.LogWarning("CSV 데이터 없음");
            return;
        }

        string outputFolder = "Assets/Resources/ItemCardAssets";

        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "ItemCardAssets");
        }

        foreach (var row in data)
        {
            ItemCardData card = ScriptableObject.CreateInstance<ItemCardData>();
            
            card.cardType = CardType.Item;
            
            card.itemName = row[0].Trim();
            card.itemDescription = row[1];
            
            string assetPath = $"{outputFolder}/{card.itemName}.asset";

            AssetDatabase.CreateAsset(card, assetPath);
            Debug.Log($"생성됨: {assetPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ItemCardScriptableObjectGenerator] 아이템 카드 생성 완료");
    }
}