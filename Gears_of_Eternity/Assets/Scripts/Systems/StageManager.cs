using System;
using UnityEngine;

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

    void Start() {
        StageFlow.Instance.GenerateNew(seed: 12345);
        FindObjectOfType<StageMapLayout>()?.Bind(StageFlow.Instance.graph);
    }
}
