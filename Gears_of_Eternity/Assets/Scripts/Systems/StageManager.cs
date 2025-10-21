using System;
using UnityEngine;
using Object = System.Object;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start() 
    {
        // TODO: 세션이 시작될 때 seed 값이 자동으로 변환하게끔 하는 로직을 추가할 것
        StageFlow.Instance.GenerateNew(seed: 22222);
        
        FindAnyObjectByType<StageMapLayout>()?.Bind(StageFlow.Instance.graph);
    }
}
