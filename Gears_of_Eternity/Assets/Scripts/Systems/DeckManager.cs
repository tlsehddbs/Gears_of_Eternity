using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public DeckData playerDeck = new DeckData();

    public bool AddCard(UnitCardData card)
    {
        if (playerDeck.cards.Count >= playerDeck.maxCardCount)
        {
            return false;
        }
        playerDeck.cards.Add(card);
        
        return true;
    }
}
