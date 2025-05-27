using UnityEngine;
using System.Collections.Generic;

using FactionTypes.Enums;
using UnitSkillTypes.Enums;

[System.Serializable]
public class SkillEffect
{
    public UnitSkillType skillType;
    public float skillValue;
    public float skillDuration; // 패시브면 0 또는 -1, 혹은 무시
}

public class SkillData : ScriptableObject
{
    [Header("Unit Skill Info")]
    public FactionType faction;
    public string unitName;
    public string skillName;
    public string skillDescription;
    public float skillCoolDown;
    
    public List<SkillEffect> effects = new List<SkillEffect>();
}
