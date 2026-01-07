using System;
using FactionTypes.Enums;
using UnitRoleTypes.Enums;
using UnityEngine;

[Serializable]
public class RuntimeUnitCard
{
    public string uniqueId;
    
    public string unitName;
    public string unitDescription;
    
    public FactionType faction;
    public RoleTypes roleTypes;
    public GameObject unitPrefab;
    
    public float health;
    public float defense;
    
    public float moveSpeed;

    public string attackType;
    public float attackValue;
    public float attackSpeed;
    public float attackDistance;
    
    public int level;
    
    
    public RuntimeUnitCard(UnitCardData data)
    {
        uniqueId = Guid.NewGuid().ToString();
        
        
        unitName = data.unitName;
        unitDescription = data.description;
        
        faction = data.faction;
        
        roleTypes = data.roleType;
        unitPrefab = data.unitPrefab;
        
        health = data.health;
        defense = data.defense;
        
        moveSpeed = data.moveSpeed;

        attackType = data.attackType;
        attackValue = data.attackValue;
        attackSpeed = data.attackSpeed;
        attackDistance = data.attackDistance;
        
        level = data.level;
        
        // Skill ~~
    }
}