using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class CSVLoader : MonoBehaviour
{
    // ReSharper disable Unity.PerformanceAnalysis
    public static List<string[]> LoadCSV(string path)
    {
        List<string[]> csvData = new List<string[]>();
        TextAsset csvFile = Resources.Load<TextAsset>(path);

        if (csvFile == null)
        {
            Debug.LogError($"파일 로드 실패: {path}");
            return csvData;
        }

        StringReader reader = new StringReader(csvFile.text);

        bool isFirstLine = true;
        while (reader.Peek() != -1)
        {
            string line = reader.ReadLine();

            // 첫 번째 헤더 행 스킵
            if (isFirstLine)
            {
                isFirstLine = false;
                continue;
            }

            string[] fields = line.Split(',');
            csvData.Add(fields);
        }

        return csvData;
    }
}