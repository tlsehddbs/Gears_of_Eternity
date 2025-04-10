using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitCard", menuName = "Card/UnitCard")]
public class UnitCardData : BaseCardData
{
    public GameObject unitPrefab;
    public short health;
    public short attack;
    public short defense;
    public short mana;
    public short speed;
}
