using System.Collections.Generic;

[System.Serializable]
public class DeckData
{
    public List<UnitCardData> cards = new();
    public int maxCardCount = 10;
}
