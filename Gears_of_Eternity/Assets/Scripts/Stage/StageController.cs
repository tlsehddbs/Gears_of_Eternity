using UnityEngine;

public static class StageContext
{
    public static BaseStageData CurrentStage { get; private set; }
    
    public static void Set(BaseStageData def) => CurrentStage = def;
    public static void Clear() => CurrentStage = null;
}

public class StageController : MonoBehaviour
{
    private async void OnStageEnd(bool IsCleared = true)
    {
        Debug.Log((IsCleared ? "Cleared Stage" : "Loaded Stage"));
        await StageFlow.Instance.OnStageEnd(IsCleared);
    }

#if UNITY_EDITOR
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            OnStageEnd(false);
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            OnStageEnd();
        }
    }
#endif
}
