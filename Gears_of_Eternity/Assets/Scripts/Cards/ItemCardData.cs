using UnityEngine;
using ItemEffectTypes.Enums;

[CreateAssetMenu(fileName = "NewItemCard", menuName = "Card/ItemCard")]
public class ItemCardData : BaseCardData
{
    public string itemName;
    public string itemDescription;
    
    public ItemGroupType itemGroup;
    
    public ItemEffectType effectType;   // 아이템 분류용
                                        // 아이템 효과 관련 enum??(토의 후 결정)
    public float effectValue;
    public float effectDuration;
    
    public ItemTriggerConditionType itemTriggerCondition;
    public ItemTargetType itemTargetType;
}

