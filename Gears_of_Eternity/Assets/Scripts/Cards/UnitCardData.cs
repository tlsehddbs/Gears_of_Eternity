using UnityEngine;
using System.Collections.Generic;

using BattleTypes.Enums;

[CreateAssetMenu(fileName = "NewUnitCard", menuName = "Card/UnitCard")]
public class UnitCardData : BaseCardData
{
    [Space(10)]
    [Header("Unit Card Info")]
    public string unitName;
    // public string description;
    public BattleType battleType;
    public GameObject unitPrefab;
    
    public float health;
    public float defense;
    
    public float moveSpeed;
    
    public float attack;
    public float attackSpeed;
    public float attackRange;
    public float attackDistance;

    [Space(10)]
    [Header("Unit Skill Info")]
    public string skillName;
    public float skillCoolDown;
    public string skillDescription;
    
    [Space(10)]
    public int level;
    
    public List<UnitCardData> nextUpgrades;
}
