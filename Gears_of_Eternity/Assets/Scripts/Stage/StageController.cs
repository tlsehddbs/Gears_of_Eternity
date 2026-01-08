using UnityEngine;

public static class StageContext
{
    public static BaseStageData CurrentStage { get; private set; }
    
    public static void Set(BaseStageData def) => CurrentStage = def;
    public static void Clear() => CurrentStage = null;
}

public class StageController : MonoBehaviour
{
    public async void OnStageEnd()
    {
        await StageFlow.Instance.OnStageEnd();
    }

#if UNITY_EDITOR
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            OnStageEnd();
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            PlayerState.Instance.TryGetRandomUpgradeableCard(out var card);
        }
    }
#endif
}
