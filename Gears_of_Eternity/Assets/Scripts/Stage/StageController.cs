using TMPro;
using System.Collections;
using UnityEngine;

public static class StageContext
{
    public static BaseStageData CurrentStage { get; private set; }
    
    public static void Set(BaseStageData def) => CurrentStage = def;
    public static void Clear() => CurrentStage = null;
}


public class StageController : MonoBehaviour
{
    private Coroutine _co;
    
    private async void OnStageEnd(bool isCleared = true)
    {
        if (_co != null)
        {
            StartCoroutine(DeckDrawLoop());
        }
        
        Debug.Log((isCleared ? "Cleared Stage" : "Loaded Stage"));
        
        await StageFlow.Instance.OnStageEnd(isCleared);
    }

    private void Start()
    {
        DeckManager.Instance.ResetCost();
        
        DeckManager.Instance.DrawCards(4);
    }

    void OnEnable()
    {
        _co = StartCoroutine(DeckDrawLoop());
    }

    // void OnDisable()
    // {
    //     if (_co != null)
    //     {
    //         StartCoroutine(DeckDrawLoop());
    //     }
    // }

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

    private IEnumerator DeckDrawLoop()
    {
        var wait = new WaitForSecondsRealtime(20f);
        while (true)
        {
            DeckManager.Instance.ResetCost();
            DeckManager.Instance.DrawCards(4);
            yield return wait;
        }
    }
}

