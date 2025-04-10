using UnityEngine;

[CreateAssetMenu(fileName = "NewItemCard", menuName = "Card/ItemCard")]
public class ItemCardData : BaseCardData
{
    public ItemeEffectType effectType;
    public float effectValue;
    public float effectDuration;
}

public enum ItemeEffectType
{
    Combat,
    Buff,
    Strategy,
    Passive,
    Consumable
}
