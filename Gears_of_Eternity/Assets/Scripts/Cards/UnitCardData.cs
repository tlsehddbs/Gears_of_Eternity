using UnityEngine;
using System.Collections.Generic;

using UnitRoleTypes.Enums;
//using UnitSkillTypes.Enums;


[CreateAssetMenu(fileName = "NewUnitCard", menuName = "Card/UnitCard")]
public class UnitCardData : BaseCardData
{
    [Space(10)] 
    [Header("Unit Card Info")] 
    //public string cardId;
    public string unitName;
    public string description;
    public RoleTypes roleType;
    public GameObject unitPrefab;
    
    public float health;
    public float defense;
    
    public float moveSpeed;
    
    public float attackType;
    public float attackValue;
    public float attackSpeed;
    public float attackDistance;

    
    [Space(10)]
    public int level;
    
    public List<UnitCardData> nextUpgrades;
}
