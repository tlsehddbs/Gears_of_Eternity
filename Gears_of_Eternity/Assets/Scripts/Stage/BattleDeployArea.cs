using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BattleDeployArea : MonoBehaviour
{
    [SerializeField] public Transform[] slots;

    private void Awake()
    {
        EnemySpawnSystem.Instance.Init();
    }

    public IReadOnlyList<Transform> GetSlotsShuffled()
    {
        var list = new List<Transform>(slots);
        
        var rng = new System.Random(Random.Range(100, 2000));

        for (int i = 0; i < list.Count; i++)
        {
            int j = rng.Next(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }
}
