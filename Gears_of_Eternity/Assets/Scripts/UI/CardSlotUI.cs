using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardSlotUI : MonoBehaviour, IPointerClickHandler
{
    public Text cardNameText;
    //public Text costText;
    public RuntimeUnitCard cardData;

    public void Initialize(RuntimeUnitCard card)
    {
        cardData = card;
        //cardNameText.text = card.unitName;
        //costText.text = card.cost.ToString();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        DeckManager.Instance.UseCard(cardData);
        Destroy(gameObject);
    }
}