using UnityEngine;

public static class StageContext
{
    public static BaseStageData CurrentStage { get; private set; }
    
    public static void Set(BaseStageData def) => CurrentStage = def;
    public static void Clear() => CurrentStage = null;
}

public class StageController : MonoBehaviour
{
    public async void OnStageCleared()
    {
        await StageFlow.Instance.OnStageCleared();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            OnStageCleared();
        }
    }
}
