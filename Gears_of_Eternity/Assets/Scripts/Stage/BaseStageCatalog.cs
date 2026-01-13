using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Stage/Base Stage Catalog")]
public class BaseStageCatalog : ScriptableObject
{
    public List<BaseStageData> stages = new();

    public BaseStageData GetByType(StageTypes.StageNodeTypes t)
    {
        // 현재 단순 매칭. 추후 type별 후보 중 랜덤 리턴하도록 변경도 가능
        return stages.Find(s => s.type == t);
    }
}
