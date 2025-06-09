using System.Collections.Generic;
using UnitSkillTypes.Enums;
using Unity.Android.Gradle.Manifest;
using UnityEngine;

public class SkillExecutor
{
    private readonly Dictionary<UnitSkillType, ISkillBehavior> behaviorMap = new Dictionary<UnitSkillType, ISkillBehavior>
    {
        { UnitSkillType.InstantHeal, new InstantHealSkill() },
        { UnitSkillType.IncreaseAttack, new BuffAttackSkill() },
        { UnitSkillType.AttackDown, new DebuffAttackSkill() },
        { UnitSkillType.MultiHit, new MultiHitSkill()},
        { UnitSkillType.Barrier, new BarrierOnHpHalfSkill()},
       
        // 추가 스킬은 여기에 등록
    };

    public void ExecuteSkill(SkillData skillData, UnitCombatFSM caster, UnitCombatFSM target)
    {
        if (skillData == null || skillData.effects == null) return;
        foreach (var effect in skillData.effects)
        {
            if (behaviorMap.TryGetValue(effect.skillType, out var behavior))
            {
                behavior.Execute(caster, target, effect);
            }
            else
            {
                Debug.LogWarning($"[SkillExecutor] 미구현 스킬 타입: {effect.skillType}");
            }
        }
    }
}