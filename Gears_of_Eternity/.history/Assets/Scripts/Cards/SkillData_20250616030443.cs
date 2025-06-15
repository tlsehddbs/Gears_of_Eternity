using UnityEngine;
using System.Collections.Generic;
using FactionTypes.Enums;
using UnitSkillTypes.Enums;

[System.Serializable]
public class SkillEffect
{
    public UnitSkillType skillType; //스킬 Enum 타입 
    public BuffStat buffStat;       //적용할 스탯들 
    public float skillValue;        // 스킬 수치 값 ex) 0.1 > 10%
    public float skillDuration;     // 0(영구) 또는 임시 
    public float skillDelayTime;  
    public float skillMaxStack;  //스킬 발동하기까지 딜레이 시간 
    public bool isPercent;    
         //true: 비율(곱셈), false: 가산(절대값)으로 계산 
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
