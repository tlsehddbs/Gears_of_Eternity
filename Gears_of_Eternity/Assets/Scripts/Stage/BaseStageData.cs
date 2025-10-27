using UnityEngine;

[CreateAssetMenu(menuName = "Stage/Base Stage Data Definition")]
public class BaseStageData : ScriptableObject
{
    [Header("Identity")] 
    public string id;  // (guid) Optional
    public StageTypes.StageNodeTypes type;

    [Header("Addressable/Scene")] 
    public string addressableKey;       // Additive로 로딩할 씬/ 프리팹 키
    
    [Header("Tuning")]
    public AnimationCurve difficultyCurve;      // 깊이에 따라 난이도 보정
    public float baseWeight = 1f;               // 가중치  
}
