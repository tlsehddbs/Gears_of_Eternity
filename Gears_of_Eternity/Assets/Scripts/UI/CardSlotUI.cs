using UnityEngine;
using UnityEngine.UI;

public class CardSlotUI : MonoBehaviour
{
    public Text cardNameText;
    public Text cardDescriptionText;
    
    public RuntimeUnitCard CardData { get; private set; }

    public void Initialize(RuntimeUnitCard data)
    {
        CardData = data;
        GetComponent<CardDragHandler>().cardData = data;
        
        cardNameText.text = data.unitName;
        cardDescriptionText.text = data.unitDescription;
        
        //costText.text = card.cost.ToString();

    }
}