using UnityEngine;

public class BaseCardData : ScriptableObject
{
    public string cardId;
    public string cardName;
    public string cardDescription;
    public Sprite cardIcon;
    public CardType cardType;
    public Faction faction;
    public Rarity rarity;
    public int cost;
}

public enum CardType
{
    Unit,
    Item
}

public enum Faction
{
    IronGearFederation,
    BrassChimera,
    ClockworkOracle,
    AshenSteamSyndicate
}

public enum Rarity
{
    Common,
    Rare,
    Legendary
}
