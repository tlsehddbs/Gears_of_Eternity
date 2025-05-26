using System.Collections.Generic;
using UnitSkillTypes.Enums;
using UnityEngine;

public class SkillExecutor
{
    private readonly Dictionary<UnitSkillType, ISkillBehavior> behaviorMap = new Dictionary<UnitSkillType, ISkillBehavior>
    {
        { UnitSkillType.InstantHeal, new InstantHealSkill() },
        { UnitSkillType.IncreaseAttack, new BuffAttackSkill() },
        { UnitSkillType.AttackDown, new DebuffAttackSkill() }
        // 추가 스킬은 여기에 등록
    };

    public void ExecuteSkill(SkillData skillData, UnitCombatFSM caster, UnitCombatFSM target)
    {
        if (skillData == null) return;
        if (behaviorMap.TryGetValue(skillData.skillType, out var behavior))
        {
            behavior.Execute(caster, target, skillData);
        }
        else
        {
            Debug.LogWarning($"[SkillExecutor] 미구현 스킬 타입: {skillData.skillType}");
        }
    }
}