using System.Collections.Generic;
using UnitSkillTypes.Enums;
//using Unity.Android.Gradle.Manifest;
using UnityEngine;

public class SkillExecutor
{
    private static readonly Dictionary<UnitSkillType, ISkillBehavior> behaviorMap = new()
    {
        { UnitSkillType.InstantHeal, new InstantHealSkill()},
        //{ UnitSkillType.IncreaseAttack, new BuffAttackSkill()},
        //{ UnitSkillType.AttackDown, new DebuffAttackSkill()},
        { UnitSkillType.MultiHit, new MultiHitSkill()},
        { UnitSkillType.BarrierOnHpHalf, new BarrierOnHpHalfSkill()},
        { UnitSkillType.DashAttackAndGuard, new DashAttackAndGuardSkill()},
        { UnitSkillType.ThrowSpearAttack, new ThrowSpearAttackSkill()},
        { UnitSkillType.ConeTripleHit, new ConeTripleHitSkill()},
        { UnitSkillType.BleedBurst, new BleedBurstSkill()},
        { UnitSkillType.DoubleAttack, new DoubleAttackSkill()},
        { UnitSkillType.GrowBuffOverTime, new GrowBuffOverTimeSkill()},
        { UnitSkillType.ReflectDamage, new ReflectDamageSkill()},
        { UnitSkillType.PassiveAreaBuff, new PassiveAreaBuffSkill ()},
        { UnitSkillType.AttackSpeedUp, new AttackSpeedUpSkill ()},
        { UnitSkillType.Silence, new SilenceSkill()},
        { UnitSkillType.CriticalStrike, new CriticalStrikeSkill()},
        { UnitSkillType.HeatReactiveMark, new HeatReactiveMarkSkill()},
        { UnitSkillType.HeavyStrikeAndSlow, new HeavyStrikeAndSlowSkill()},
        { UnitSkillType.FarthestDoubleAoe, new FarthestDoubleAoeSkill()},
        { UnitSkillType.PassiveTurretBarrage, new PassiveTurretBarrageSkill()},
        { UnitSkillType.QuadFlurryBlind, new QuadFlurryBlindSkill()},
        { UnitSkillType.HealingAuraDefenseBuff, new HealingAuraDefenseBuffSKill()},
        { UnitSkillType.EmpowerZoneHighestAttackAlly, new EmpowerZoneHighestAttackAllySkill()},
        { UnitSkillType.CleanseAndShieldAoE, new CleanseAndShieldAoESkill()},
        { UnitSkillType.RectStunArmorDown, new RectStunArmorDownSkill()},
        { UnitSkillType.TripleLayerThinShield, new TripleLayerThinShieldSkill()},
        { UnitSkillType.ThrowSpearPoisonAttack, new ThrowSpearPoisonAttackSkill()},
        { UnitSkillType.HealOverTime, new HealOverTimeSkill()},
        { UnitSkillType.RegenNearbyAllies, new RegenNearbyAlliesSkill()},
        { UnitSkillType.TargetedAoeBlind, new TargetedAoeBlindSkill()},
        { UnitSkillType.RegenNearbyAlliesUpgrade, new RegenNearbyAlliesUpgradeSkill()},
        { UnitSkillType.NearestEnemyAoeStunThenBlind, new NearestEnemyAoeStunThenBlindSkill()},
        { UnitSkillType.DefenseAndDamageReductionSelfBuff, new DefenseAndDamageReductionSelfBuffSkill()},
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
        //Debug.Log($"[TryUseSkillIfPossible] enter unit={caster.name} stunned={caster.IsStunned()} silenced={caster.isSilenced} canUse={caster.CanUseSkill()}");
        if (caster.IsStunned()) return false;

        if (caster.isSilenced) return false;

        if (skillData == null || skillData.effects == null || !caster.CanUseSkill()) return false;

        foreach (var effect in skillData.effects)
        {
            if (!behaviorMap.TryGetValue(effect.skillType, out var behavior)) continue;
            if (!behavior.ShouldTrigger(caster, effect))
            {
                caster.FindNewTarget();
                continue;
            }

            float castTime = effect.skillDelayTime > 0f ? effect.skillDelayTime : 0.25f;

            var target = behavior.FindTarget(caster, effect);
            if (target == null) continue;

            if (effect.skillRange <= 0f || target == caster)
            {
                
                SkillCastVfxManager.Instance?.PlayCast(caster, effect.skillType, castTime);    
                
                behavior.Execute(caster, target, effect);
                caster.skillTimer = 0f;
                return true;
            }

            float dist = Vector3.Distance(caster.transform.position, target.transform.position);
            float range = effect.skillRange;
            //Debug.Log($"[SkillFlow] type={effect.skillType} dist={dist:F2} range={range:F2}");

            // 사거리 밖이면 이동만 준비
            //Debug.Log($"[SkillFlow] MOVE_ONLY -> target={(target!=null?target.name:"null")}");
            if (dist > range)
            {
                if (target.unitData.faction == caster.unitData.faction)
                {
                    caster.targetAlly = target;
                }
                else
                {
                    caster.targetEnemy = target;
                }
                return true;
            }
            // 사거리 안이면 즉시 시전
            //스킬 VFX
            SkillCastVfxManager.Instance?.PlayCast(caster, effect.skillType, castTime);

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

            // float dist = Vector3.Distance(caster.transform.position, target.transform.position);
            // float range = caster.stats.attackDistance * 1.5f;

            // // 💡 사거리 밖이면 접근 필요
            // if (dist > range)
            // {
            //     caster.targetAlly = target;
            //     return true;
            // }

            if (effect.skillRange <= 0f || target == caster)
                continue;

            float dist = Vector3.Distance(caster.transform.position, target.transform.position);
            float range = effect.skillRange; 
            
            if (dist > range)
            {
                if (target.unitData.faction == caster.unitData.faction)
                    caster.targetAlly = target;
                else
                    caster.targetEnemy = target;
                return true;
            }
        }

        return false;
    }

}