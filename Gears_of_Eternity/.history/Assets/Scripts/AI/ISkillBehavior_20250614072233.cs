using System.Collections;
using UnityEngine;

public interface ISkillBehavior
{
    bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect);
    UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect);
    void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect);
    void Remove(UnitCombatFSM caster, SkillEffect effect); // 패시브 해제용
}



// 즉시 힐 스킬 
public class InstantHealSkill : ISkillBehavior
{
    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        var target = FindTarget(caster, effect);
        return target != null && target.currentHP < target.stats.health && caster.CanUseSkill();
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        return caster.FindLowestHpAlly();
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        target?.ReceiveHealing(effect.skillValue);
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
}

// public class BuffAttackSkill : ISkillBehavior
// {
//     public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
//     {
//         return FindTarget(caster, effect) != null && caster.CanUseSkill();
//     }

//     public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
//     {
//         return caster.FindNearestAlly();
//     }

//     public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
//     {
//         target?.ApplyBuff(effect.buffStat, effect.skillValue, effect.skillDuration, effect.isPercent);
//     }

//     public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
// }

// public class DebuffAttackSkill : ISkillBehavior
// {
//     public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
//     {
//         return FindTarget(caster, effect) != null && caster.CanUseSkill();
//     }

//     public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
//     {
//         return caster.FindNearestEnemy();
//     }

//     public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
//     {
//         target?.ApplyDebuff(effect.buffStat, effect.skillValue, effect.skillDuration);
//     }

//     public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
// }


// 연속 타격 스킬 / 터빈 절삭자 
public class MultiHitSkill : ISkillBehavior
{
    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        return FindTarget(caster, effect) != null && caster.CanUseSkill();
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        return caster.FindNearestEnemy();
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null || target == null) return;
        caster.StartCoroutine(MultiHitRoutine(caster, target, effect));
    }

    private IEnumerator MultiHitRoutine(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        int hitCount = 3;
        float damagePercent = effect.skillValue;
        float delay = 0.2f;

        for (int i = 0; i < hitCount; i++)
        {
            float damage = caster.stats.attack * damagePercent;
            target.TakeDamage(damage);
            yield return new WaitForSeconds(delay);
        }
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
}

public class BarrierOnHpHalfSkill : ISkillBehavior
{
    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        return caster.CanUseSkill() && caster.currentHP / caster.stats.health <= 0.5f && caster.stats.barrier <= 0.01f;
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        return caster; // 자기 자신 대상
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        float barrierAmount = caster.stats.health * effect.skillValue;
        caster.ApplyBarrier(barrierAmount, effect.skillDuration);
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
}

public class DashAttackAndGuardSkill : ISkillBehavior
{
    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect skillEffect)
    {
        // 돌진 코루틴 시작 
        caster.StartCoroutine(DashAndAttackRoutine(caster, skillEffect));
    }

    private IEnumerator DashAndAttackRoutine(UnitCombatFSM caster, SkillEffect skillEffect)
    {
        float dashDistance = 30f; //필요 시 SkillEffect/SkillData에 넣고 값으로 사용 
        float dashSpeed = 15f;
        Vector3 dashDir = caster.transform.forward;

        Vector3 targetPos = caster.transform.position + dashDir * dashDistance;
        float dashTime = dashDistance / dashSpeed;
        float t = 0f;

        while (t < dashTime)
        {
            caster.transform.position = Vector3.Lerp(caster.transform.position, targetPos, t / dashTime);
            t += Time.deltaTime;
            yield return null;
        }
        caster.transform.position = targetPos;

        float range = 3.0f; // 돌진 경로 폭 
        Collider[] hits = Physics.OverlapBox(targetPos, new Vector3(range, 1, dashDistance / 2), caster.transform.rotation);

        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<UnitCombatFSM>();
            //유닛인지 체크, 적군 판별 
            if (enemy != null && enemy.unitData.faction != caster.unitData.faction)
            {
                float damage = caster.stats.attack * skillEffect.skillValue;
                enemy.TakeDamage(damage);
                Debug.Log($"[돌진공격] {enemy.name}에게 {damage} 데미지!");
            }
        }

        caster.stats.guardCount += 3;
    }
    public void Remove(UnitCombatFSM caster, SkillEffect skillEffect) { }
}

//창 투척 스킬 /기어 창 투척병 
public class ThrowSpearAttackSkill : ISkillBehavior
{
    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        var target = FindTarget(caster, effect);
        if (target == null) return false;

        float dist = Vector3.Distance(caster.transform.position, target.transform.position);
        float range = caster.stats.attackDistance * 3f;
        return dist <= range && caster.CanUseSkill();
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        return caster.FindNearestEnemy();
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null || target == null) return;
        float damage = caster.stats.attack * effect.skillValue;
        target.TakeDamage(damage);
        Debug.Log($"[창 투척] {caster.name} → {target.name}에게 {damage:F1} 피해");
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
}