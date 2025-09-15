using System;
using BattleTypes.Enums;
using FactionTypes.Enums;
using UnityEngine;

[Serializable]
public class RuntimeUnitCard
{
    public string unitName;
    public string unitDescription;
    
    public FactionType faction;
    public BattleType battleType;
    public GameObject unitPrefab;
    
    public float health;
    public float defense;
    
    public float moveSpeed;

    public float attack;
    public float attackSpeed;
    public float attackRange;
    public float attackDistance;
    
    public int level;
    
    
    
    public string uniqueId;


    
    public RuntimeUnitCard(UnitCardData data)
    {
        uniqueId = Guid.NewGuid().ToString();
        
        
        unitName = data.unitName;
        unitDescription = data.description;
        
        faction = data.faction;
        
        battleType = data.battleType;
        unitPrefab = data.unitPrefab;
        
        health = data.health;
        defense = data.defense;
        
        moveSpeed = data.moveSpeed;
        
        attack = data.attack;
        attackSpeed = data.attackSpeed;
        attackRange = data.attackRange;
        attackDistance = data.attackDistance;
        
        level = data.level;
        
        // Skill ~~
    }
}