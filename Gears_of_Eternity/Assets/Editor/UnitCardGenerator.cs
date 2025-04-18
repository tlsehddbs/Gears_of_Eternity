using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class UnitCardGenerator : EditorWindow
{
    [MenuItem("Tools/Automatic Card Generator/Unit Cards")]
    public static void ShowWindow()
    {
        GetWindow<UnitCardGenerator>("Unit Card Generator");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("유닛 카드 자동 생성 (CSV → ScriptableObject)"))
        {
            GenerateUnitCards();
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void GenerateUnitCards()
    {
        string csvPath = "CSV/UnitCardData"; // Resources/CSV/aa.csv
        List<string[]> data = CSVLoader.LoadCSV(csvPath);

        if (data == null || data.Count == 0)
        {
            Debug.LogWarning("CSV 데이터가 없습니다.");
            return;
        }

        string outputFolder = "Assets/Resources/UnitCardAssets";

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

            UnitCardData card = ScriptableObject.CreateInstance<UnitCardData>();
            card.unitName = row[0];
            card.description = row[1];
            
            card.health = float.Parse(row[2]);
            card.attack = float.Parse(row[3]);
            card.attackRange = float.Parse(row[4]);
            card.attackSpeed = float.Parse(row[5]);
            card.defense = float.Parse(row[6]);
            card.mana = float.Parse(row[7]);
            card.speed = float.Parse(row[8]);

            string assetPath = $"{outputFolder}/{card.unitName}.asset";

            AssetDatabase.CreateAsset(card, assetPath);
            Debug.Log($"생성됨: {assetPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ 유닛 카드 생성 완료!");
    }
}