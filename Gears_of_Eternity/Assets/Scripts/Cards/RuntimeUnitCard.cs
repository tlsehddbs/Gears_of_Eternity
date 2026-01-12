using System;
using System.Collections.Generic;
using FactionTypes.Enums;
using RarityTypes.Enums;
using UnitRoleTypes.Enums;
using UnityEngine;

[Serializable]
public class RuntimeUnitCard
{
    public string uniqueId;
    
    public string unitName;
    public string unitDescription;
    
    public FactionType faction;
    public RoleTypes roleType;
    public GameObject unitPrefab;
    
    public float health;
    public float defense;
    
    public float moveSpeed;

    public string attackType;
    public float attackValue;
    public float attackSpeed;
    public float attackDistance;
    
    public int level;
    public Rarity rarity;

    public int cost;

    public List<UnitCardData> nextUpgradeUnits;
    
    
    public RuntimeUnitCard(UnitCardData data)
    {
        uniqueId = Guid.NewGuid().ToString();
        
        
        unitName = data.unitName;
        unitDescription = data.description;
        
        faction = data.faction;
        
        roleType = data.roleType;
        unitPrefab = data.unitPrefab;
        
        health = data.health;
        defense = data.defense;
        
        moveSpeed = data.moveSpeed;

        attackType = data.attackType;
        attackValue = data.attackValue;
        attackSpeed = data.attackSpeed;
        attackDistance = data.attackDistance;
        
        level = data.level;
        rarity = data.rarity;
        
        cost = data.cost;

        nextUpgradeUnits = new List<UnitCardData>(data.nextUpgrades);
    }
}