using System;
using UnityEngine;

public class RuntimeUnitCardRef : MonoBehaviour
{
    public RuntimeUnitCard Card { get; private set; }
    public event Action<RuntimeUnitCard> OnCardChanged;

    public void SetCard(RuntimeUnitCard card)
    {
        Card = card;
        OnCardChanged?.Invoke(Card);
    }
}
