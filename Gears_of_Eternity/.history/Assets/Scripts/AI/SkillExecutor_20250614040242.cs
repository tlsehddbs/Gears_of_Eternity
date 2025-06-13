using System.Collections.Generic;
using UnitSkillTypes.Enums;
using UnityEngine;

public class SkillExecutor
{
    private readonly Dictionary<UnitSkillType, ISkillBehavior> behaviorMap = new Dictionary<UnitSkillType, ISkillBehavior>
    {
        { UnitSkillType.InstantHeal, new InstantHealSkill() },
        { UnitSkillType.IncreaseAttack, new BuffAttackSkill() },
        { UnitSkillType.AttackDown, new DebuffAttackSkill() },
        { UnitSkillType.MultiHit, new MultiHitSkill()},
        { UnitSkillType.BarrierOnHpHalf, new BarrierOnHpHalfSkill()},
        { UnitSkillType.DashAttackAndGuard, new DashAttackAndGuardSkill()},
        { UnitSkillType.ThrowSpearAttack, new ThrowSpearAttackSkill()},
       
        // 추가 스킬은 여기에 등록
    };

     public void TryUseAvailableSkill(UnitCombatFSM caster)
    {
        if (!caster.CanUseSkill() || caster.skillData == null || caster.skillData.effects == null) return;

        foreach (var effect in caster.skillData.effects)
        {
            if (behaviorMap.TryGetValue(effect.skillType, out var behavior))
            {
                if (behavior.ShouldTrigger(caster, effect))
                {
                    var target = caster.DetermineSkillTarget(effect.skillType);
                    behavior.Execute(caster, target, effect);
                    caster.skillTimer = 0f;
                    caster.isProcessingSkill = false;
                    return; // 하나만 실행
                }
            }
        }
    }






    // public void ExecuteSkill(SkillData skillData, UnitCombatFSM caster, UnitCombatFSM target)
    // {
    //     if (skillData == null || skillData.effects == null) return;
    //     foreach (var effect in skillData.effects)
    //     {
    //         if (behaviorMap.TryGetValue(effect.skillType, out var behavior))
    //         {
    //             behavior.Execute(caster, target, effect);
    //         }
    //         else
    //         {
    //             Debug.LogWarning($"[SkillExecutor] 미구현 스킬 타입: {effect.skillType}");
    //         }
    //     }
    // }
}