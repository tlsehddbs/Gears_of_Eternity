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
    public int cost;        // 유닛 소환에 사용하는 비용
    public int price;       // 아이템 등 재화로 구매할 카드의 비용
}

public enum CardType
{
    Unit,
    Item
}
