using UnityEngine;
using FactionTypes.Enums;
using RarityTypes.Enums;

public class BaseCardData : ScriptableObject
{
    [Header("Base Card Info")]
    public string id;
    public Sprite icon;
    public CardType cardType;
    public FactionType faction;
    public Rarity rarity;
    
    [Space(10)]
    public int cost;
}

public enum CardType
{
    Unit,
    Item
}
