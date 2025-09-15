using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardCollection : MonoBehaviour
{
    public List<UnitCardData> allAvailableCards;

    private void Awake()
    {
        LoadCardsFromResources();
    }

    void LoadCardsFromResources()
    {
        allAvailableCards = Resources.LoadAll<UnitCardData>("UnitCardAssets").ToList();
    }
}
