using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitCard", menuName = "Card/UnitCard")]
public class UnitCardData : BaseCardData
{
    public string unitName;
    public string description;
    public GameObject unitPrefab;
    
    public float health;
    public float attack;
    public float attackRange;
    public float attackSpeed;
    public float defense;
    public float mana;
    public float speed;
}
