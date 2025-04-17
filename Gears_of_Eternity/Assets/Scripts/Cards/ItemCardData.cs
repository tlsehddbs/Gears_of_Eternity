using UnityEngine;
using ItemEffectTypes.Enums;

[CreateAssetMenu(fileName = "NewItemCard", menuName = "Card/ItemCard")]
public class ItemCardData : BaseCardData
{
    public string itemName;
    public string itemDescription;
    
    public ItemEffectType effectType;   // 아이템 분류용
    // 아이템 효과 관련 enum??(토의 후 결정)
    
    public float effectValue;
    public float effectDuration;
}


// 아이템의 효과가 다 상이해서 하나의 generator로는 다양한 효과를 생성하는데는 한계가 있을것으로 생각됨.
// 스크립트를 복잡하게 가져가면서 아이템을 구현할지, 아이템의 효과를 변경하여 자동 생성에 조금 더 유리하게 가져갈지 고민을 해봐야겠음.