using System.Collections.Generic;
using UnitSkillTypes.Enums;
using UnityEngine;

public class SkillExecutor
{
    private static readonly Dictionary<UnitSkillType, ISkillBehavior> behaviorMap = new()
    {
        { UnitSkillType.InstantHeal, new InstantHealSkill() },
        //{ UnitSkillType.IncreaseAttack, new BuffAttackSkill() },
        //{ UnitSkillType.AttackDown, new DebuffAttackSkill() },
        { UnitSkillType.MultiHit, new MultiHitSkill()},
        { UnitSkillType.BarrierOnHpHalf, new BarrierOnHpHalfSkill()},
        { UnitSkillType.DashAttackAndGuard, new DashAttackAndGuardSkill()},
        { UnitSkillType.ThrowSpearAttack, new ThrowSpearAttackSkill()},
        { UnitSkillType.ConeTripleHit, new ConeTripleHitSkill()},
        { UnitSkillType.BleedBurst, new BleedBurstSkill() },
        { UnitSkillType.DoubleAttack, new DoubleAttackSkill() },
        { UnitSkillType.GrowBuffOverTime, new GrowBuffOverTimeSkill() },
        { UnitSkillType.ReflectDamage, new ReflectDamageSkill() },
       
        // 추가 스킬은 여기에 등록
    };


    public static ISkillBehavior GetSkillBehavior(UnitSkillType type)
    {
        behaviorMap.TryGetValue(type, out var behavior);
        return behavior;
    }

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

    public bool TryUseSkillIfPossible(UnitCombatFSM caster, SkillData skillData)
    {
        if (skillData == null || skillData.effects == null || !caster.CanUseSkill()) return false;

        foreach (var effect in skillData.effects)
        {
            if (!behaviorMap.TryGetValue(effect.skillType, out var behavior)) continue;
            if (!behavior.ShouldTrigger(caster, effect))
            {
                caster.FindNewTarget();
                continue;
            }

            var target = behavior.FindTarget(caster, effect);
            if (target == null) continue;

            behavior.Execute(caster, target, effect);
            caster.skillTimer = 0f;
            return true;
        }

        return false;
    }

    public bool ShouldMoveToSkillTarget(UnitCombatFSM caster, SkillData skillData)
    {
        if (skillData == null || skillData.effects == null || !caster.CanUseSkill()) return false;

        foreach (var effect in skillData.effects)
        {
            if (!behaviorMap.TryGetValue(effect.skillType, out var behavior)) continue;
            if (!behavior.ShouldTrigger(caster, effect)) continue;

            var target = behavior.FindTarget(caster, effect);
            if (target == null) continue;

            float dist = Vector3.Distance(caster.transform.position, target.transform.position);
            float range = caster.stats.attackDistance * 1.5f;

            // 💡 사거리 밖이면 접근 필요
            if (dist > range)
            {
                caster.targetAlly = target;
                return true;
            }
        }

        return false;
    }

}