using UnityEngine;

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
