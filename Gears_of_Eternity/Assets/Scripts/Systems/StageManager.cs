using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("Seed")] 
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int fixedSeed = 1234; 
    
    void Start() 
    {
        int seed = useRandomSeed ? Random.Range(1000, 2500) : fixedSeed;
        StageFlow.Instance.GenerateNew(seed);
        
        // Map UI Bind
        FindAnyObjectByType<StageGraphLayout>()?.Bind(StageFlow.Instance.graph);
    }
}
