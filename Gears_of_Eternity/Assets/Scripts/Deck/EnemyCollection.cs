using System.Collections.Generic;
using System.Linq;
using FactionTypes.Enums;
using UnityEngine;

public class EnemyCollection : MonoBehaviour
{
    public static EnemyCollection Instance { get; private set; }

    private List<UnitCardData> allAvailableCards = new List<UnitCardData>();
    
    public List<RuntimeUnitCard> allRuntimeCards = new List<RuntimeUnitCard>();
    
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

        LoadCardsFromResources();
    }
    
    void LoadCardsFromResources()
    {
        allAvailableCards = Resources.LoadAll<UnitCardData>("UnitCardAssets").ToList();

        foreach (var card in allAvailableCards)
        {
            var enemyRuntimeCopy = new RuntimeUnitCard(card);

            if (card != null && card.faction == FactionType.BrassChimera)
            {
                allRuntimeCards.Add(enemyRuntimeCopy);
            }
        }
    }
    
    public RuntimeUnitCard GetRandomByCost(int cost, System.Random rng)
    {
        if (rng == null) rng = new System.Random();

        // 후보를 임시로 모아서 랜덤 선택
        // (GC 줄이고 싶으면 List를 멤버로 하나 만들어 재사용해도 됨. 프로토타입이면 이대로 OK)
        List<RuntimeUnitCard> candidates = null;

        for (int i = 0; i < allRuntimeCards.Count; i++)
        {
            var data = allRuntimeCards[i];
            if (data == null) continue;

            int c = data.cost;
            if (c != cost) continue;

            candidates ??= new List<RuntimeUnitCard>();
            candidates.Add(data);
        }

        if (candidates == null || candidates.Count == 0)
            return null;

        int idx = rng.Next(0, candidates.Count);
        return candidates[idx];
    }

}
