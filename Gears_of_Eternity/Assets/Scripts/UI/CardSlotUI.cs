using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.EventSystems;

public class CardSlotUI : MonoBehaviour  //, IPointerClickHandler
{
    // public Text cardNameText;
    // public RuntimeUnitCard cardData;
    
    public RuntimeUnitCard CardData { get; private set; }

    public void Initialize(RuntimeUnitCard data)
    {
        CardData = data;
        //cardNameText.text = card.unitName;
        //costText.text = card.cost.ToString();
    }

    // public void OnPointerClick(PointerEventData eventData)
    // {
    //     DeckManager.Instance.UseCard(cardData);
    //     Destroy(gameObject);
    // }
}