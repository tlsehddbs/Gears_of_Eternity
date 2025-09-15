using UnityEngine;
using System.Collections.Generic;

using FactionTypes.Enums;
using UnitSkillTypes.Enums;

public class SkillData : ScriptableObject
{
    [Header("Unit Skill Info")] 
    public FactionType faction;
    public string unitName;
    public string skillName;
    public string skillDescription;
    
    public UnitSkillType skillType;
    public float skillValue;
    public float skillDuration;
    public float skillCoolDown;
}
