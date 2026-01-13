using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class EnemySpawnSystem : MonoBehaviour
{
    public static EnemySpawnSystem Instance { get; private set; }
    
    [Header("Refs")]
    [SerializeField] private BattleDeployArea deployArea;

    [Header("Stage")]
    private int firstStage = 1;
    private int bossStage = 15;
    [SerializeField] private int enemiesPerStage = 5;

    [SerializeField] private int minBudget = 10;
    [SerializeField] private int maxBudget = 20;

    [Tooltip("stage 1~14 사이에서 0~1 반환 (선형이면 OK)")]
    [SerializeField] private AnimationCurve budgetCurve = AnimationCurve.Linear(1, 0, 14, 1);


    public GameObject tmpPrefab;

    
    private readonly List<RuntimeUnitCard> selectedList = new();
    
    public IReadOnlyList<RuntimeUnitCard> SelectedList => selectedList;


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

    public void Init()
    {
        deployArea = GameObject.Find("EnemyDeployArea").GetComponent<BattleDeployArea>();
        
        SpawnStage();
    }

    public void Reset()
    {
        selectedList.Clear();
    }

    public void SpawnStage()
    {
        Reset();
        
        if (deployArea == null)
        {
            Debug.LogError("[CostBasedStageSpawner] deployArea is null.");
            return;
        }

        var stage = StageFlow.Instance.graph.FindNode(StageFlow.Instance.graph.currentNodeId).layerIndex + 1;
        Debug.Log($"현제 레이어 : {stage}");

        if (stage >= bossStage)
            return;

        var slots = deployArea.GetSlotsShuffled();
        if (slots == null || slots.Count == 0)
        {
            Debug.LogError("[CostBasedStageSpawner] deployArea slots are empty.");
            return;
        }

        int spawnCount = Mathf.Min(enemiesPerStage, slots.Count);
        if (spawnCount < enemiesPerStage)
            Debug.LogWarning($"[CostBasedStageSpawner] Not enough slots. Need {enemiesPerStage}, but got {slots.Count}. Spawning {spawnCount}.");

        int budget = GetBudget(stage);
        int minPossible = 2 * spawnCount;
        int maxPossible = 4 * spawnCount;
        budget = Mathf.Clamp(budget, minPossible, maxPossible);     // 5마리 고정이므로 예산을 가능 범위로 clamp해서 "최대한 남김없이" 성립시키기

        // 최적 cost 조합 뽑기
        var combo = PickBestCombo(spawnCount, budget);

        // cost 리스트 -> 셔플 -> 슬롯 순서대로 스폰
        var costsToSpawn = BuildCostList(combo, spawnCount);
        
        var rng = new System.Random(Random.Range(100, 2000) * 9176 + stage * 101);
        Shuffle(costsToSpawn, rng);

        for (int i = 0; i < spawnCount; i++)
        {
            int cost = costsToSpawn[i];

            RuntimeUnitCard picked = EnemyCollection.Instance.GetRandomByCost(cost, rng);
            if (picked == null)
            {
                Debug.LogError("[EnemySpawnSystem] No available RuntimeUnitData to spawn. Abort.");
                return;
            }

            selectedList.Add(picked);
        }

        for (int i = 0; i < selectedList.Count && i < slots.Count; i++)
        {
            var data = selectedList[i];

            Instantiate(data.unitPrefab ? data.unitPrefab : tmpPrefab, slots[i].position, slots[i].rotation);
            Debug.Log($" 적 ai 생성됨 : {data.unitName} / Level : {data.level} /  Cost : {data.cost}");
        }
    }

    private int GetBudget(int stage)
    {
        float t = Mathf.Clamp01(budgetCurve.Evaluate(stage)); // 0..1
        return Mathf.RoundToInt(Mathf.Lerp(minBudget, maxBudget, t));
    }

    private (int n2, int n3, int n4, int sum) PickBestCombo(int count, int budget)
    {
        int bestSum = -1;
        var best = new List<(int n2, int n3, int n4, int sum)>();

        for (int n2 = 0; n2 <= count; n2++)
        {
            for (int n3 = 0; n3 <= count - n2; n3++)
            {
                int n4 = count - n2 - n3;
                int sum = 2 * n2 + 3 * n3 + 4 * n4;

                if (sum > budget) continue;

                if (sum > bestSum)
                {
                    bestSum = sum;
                    best.Clear();
                    best.Add((n2, n3, n4, sum));
                }
                else if (sum == bestSum)
                {
                    best.Add((n2, n3, n4, sum));
                }
            }
        }

        // count=5, budget clamp(10~20)이면 best가 비는 일은 거의 없지만 안전 처리
        if (best.Count == 0)
            return (count, 0, 0, 2 * count);

        return best[Random.Range(0, best.Count)];
    }

    private static List<int> BuildCostList((int n2, int n3, int n4, int sum) combo, int count)
    {
        var list = new List<int>(count);
        for (int i = 0; i < combo.n2; i++) list.Add(2);
        for (int i = 0; i < combo.n3; i++) list.Add(3);
        for (int i = 0; i < combo.n4; i++) list.Add(4);
        return list;
    }

    private static void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = rng.Next(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

