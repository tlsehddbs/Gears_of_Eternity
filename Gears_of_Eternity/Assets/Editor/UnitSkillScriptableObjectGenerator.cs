using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

using FactionTypes.Enums;
using ItemEffectTypes.Enums;
using UnitSkillTypes.Enums;

public class UnitSkillScriptableObjectGenerator : EditorWindow
{
    [MenuItem("Tools/Automatic Skill Generator/Skill Data")]
    public static void ShowWindow()
    {
        GetWindow<UnitSkillScriptableObjectGenerator>("Skill Card Generator");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("유닛 스킬 데이터 자동 생성 (CSV → ScriptableObject)"))
        {
            GenerateSkillData();
        }
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    private void GenerateSkillData()
    {
        string csvPath = "CSV/UnitSkillData";
        List<string[]> data = CSVLoader.LoadCSV(csvPath);

        if (data == null || data.Count == 0)
        {
            Debug.LogWarning("CSV 데이터 없음");
            return;
        }

        string outputFolder = "Assets/Resources/SkillDataAssets";

        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "SkillDataAssets");
        }

        foreach (var row in data)
        {
            // 유효성 검사
            if (row.Length < 6)
            {
                Debug.LogWarning("유효하지 않은 데이터(필드 부족)");
                continue;
            }

            SkillData skillData = ScriptableObject.CreateInstance<SkillData>();

            skillData.faction = (FactionType)Enum.Parse(typeof(FactionType), row[0]);
            skillData.unitName = row[1].Trim();
            skillData.skillDescription = row[2];
            //skillData.skillType = (UnitSkillType)Enum.Parse(typeof(UnitSkillType), row[3]);
            
            //skillData.skillValue = float.Parse(row[4]);
            //skillData.skillDuration = float.Parse(row[5]);
            skillData.skillCoolDown = float.Parse(row[3]);

            string assetPath = $"{outputFolder}/Skill_{skillData.unitName}.asset";

            AssetDatabase.CreateAsset(skillData, assetPath);
            Debug.Log($"생성됨: {assetPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[UnitSkillScriptableObjectGenerator] 유닛 스킬 데이터 생성 완료");
    }
}
