using System.Collections;
using System.Collections.Generic;
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
    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        var target = FindTarget(caster, effect);
        if (target == null) return false;

        float dist = Vector3.Distance(caster.transform.position, target.transform.position);
        return caster.CanUseSkill() && dist <= 30f;
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        return caster.FindNearestEnemy();
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        caster.StartCoroutine(DashAndAttackRoutine(caster, effect));
    }

    private IEnumerator DashAndAttackRoutine(UnitCombatFSM caster, SkillEffect effect)
    {
        float dashDistance = 30f;
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

        float range = 3.0f;
        Collider[] hits = Physics.OverlapBox(targetPos, new Vector3(range, 1, dashDistance / 2), caster.transform.rotation);

        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<UnitCombatFSM>();
            if (enemy != null && enemy.unitData.faction != caster.unitData.faction)
            {
                float damage = caster.stats.attack * effect.skillValue;
                enemy.TakeDamage(damage);
            }
        }

        caster.stats.guardCount += 3;
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
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


public class ConeTripleHitSkill : ISkillBehavior
{
    private const int hitCount = 3;
    private const float hitDelay = 0.25f;
    private const float Angle = 90f;
    private const float RangeMultiplier = 10f;

    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {


        var list = caster.FindEnemiesInCone(Angle, RangeMultiplier);
        return list.Count > 0;
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        return caster.FindNearestEnemy(); // 대표 타겟 하나만 반환
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        caster.StartCoroutine(ExecuteTripleHit(caster, effect));
        caster.skillTimer = 0f;
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }

    private IEnumerator ExecuteTripleHit(UnitCombatFSM caster, SkillEffect effect)
    {
        //Debug.Log("[ConeTripleHit] 코루틴 시작됨");

        for (int i = 0; i < hitCount; i++)
        {
            var targets = caster.FindEnemiesInCone(Angle, RangeMultiplier);
            //Debug.Log($"[ConeTripleHit] {i+1}회차 대상 수: {targets.Count}");
            foreach (var enemy in targets)
            {
                float damage = caster.stats.attack * effect.skillValue;
                enemy.TakeDamage(damage);
                Debug.Log($"[ConeTripleHit] {enemy.name} → {damage:F1} 피해 (타격 {i + 1}/3)");
            }
            yield return new WaitForSeconds(hitDelay);
        }
    }
}

//최대 8회(0.25초 간격) 1회당 공격력40% + 고정 피해 20% 맞은 적 출혈 시전 중 자신은 방어력 0 / 절삭 기어혼 
public class BleedBurstSkill : ISkillBehavior
{
    private const int maxHits = 8;
    private const float hitDelay = 0.25f;
    private const float fixedDamage = 20f;
    private const float castTime = 2.0f;
    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        var target = caster.FindNearestEnemy();
        if (target == null) return false;

        float dist = Vector3.Distance(caster.transform.position, target.transform.position);
        float range = caster.stats.attackDistance * 5f;

        return dist <= range; // 사거리 안에 있는 적만 스킬 발동
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        return caster.FindNearestEnemy();
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        caster.StartCoroutine(ExecuteHits(caster, effect));
        caster.skillTimer = 0f;
    }
    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }


    private IEnumerator ExecuteHits(UnitCombatFSM caster, SkillEffect effect)
    {
        float originalDefense = caster.stats.defense;
        caster.stats.defense = 0f;
        Debug.Log($"[BloodRend] {caster.name} → 방어력 0으로 설정");

        for (int i = 0; i < maxHits; i++)
        {
            var target = caster.FindNearestEnemy();
            if (target != null)
            {
                float totalDamage = caster.stats.attack * effect.skillValue + fixedDamage;
                target.TakeDamage(totalDamage);
                BleedSystem.ApplyBleed(target);
                Debug.Log($"[BloodRend] {target.name} 타격 {i + 1}/8 → {totalDamage:F1} 피해 및 출혈 적용");
            }
            yield return new WaitForSeconds(hitDelay);
        }

        yield return new WaitForSeconds(castTime - maxHits * hitDelay);
        caster.stats.defense = originalDefense;
        Debug.Log($"[BloodRend] {caster.name} → 방어력 복구됨: {originalDefense}");
    }
}

public class DoublicAttackSkill : ISkillBehavior
{
    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        return true; // 지속형 패시브는 항상 적용됨
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        return null; // 타겟 없음
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        caster.OnPostAttack += () =>
        {
            if (caster.targetEnemy != null && caster.targetEnemy.IsAlive())
            {
                float secondHit = caster.stats.attack * effect.skillValue;
                caster.targetEnemy.TakeDamage(secondHit);
                Debug.Log($"[DoubleAttackSkill] 추가 타격: {secondHit:F1} 피해");
            }
        };
    }
}






//출혈 로직 /현재 체력 비례 /최대 중첩3(중첩당 1초 증가)
public static class BleedSystem
{
    private class BleedStatus
    {
        public int stack;
        public Coroutine routine;
    }

    private static readonly Dictionary<UnitCombatFSM, BleedStatus> activeBleeds = new();

    public static void ApplyBleed(UnitCombatFSM target, float durationPerStack = 3f, int maxStack = 3, float percentPerSec = 0.1f)
    {
        if (!target.IsAlive()) return;

        if (!activeBleeds.TryGetValue(target, out var status))
        {
            status = new BleedStatus { stack = 1 };
            status.routine = target.StartCoroutine(BleedRoutine(target, percentPerSec, durationPerStack, status));
            activeBleeds[target] = status;
        }
        else
        {
            status.stack = Mathf.Min(status.stack + 1, maxStack);
        }
    }

    private static IEnumerator BleedRoutine(UnitCombatFSM target, float percentPerSec, float durationPerStack, BleedStatus status)
    {
        float totalTime = 0f;
        while (status.stack > 0)
        {
            float duration = durationPerStack + (status.stack - 1); // 중첩당 시간 증가
            float tickTime = 1f;
            while (totalTime < duration)
            {
                if (!target.IsAlive()) yield break;

                float bleedDmg = target.currentHP * percentPerSec;
                target.TakeDamage(bleedDmg);
                Debug.Log($"[출혈] {target.name} → {bleedDmg:F1} 출혈 피해 ({status.stack}중첩)");
                yield return new WaitForSeconds(tickTime);
                totalTime += tickTime;
            }
            status.stack--;
            totalTime = 0f;
        }

        activeBleeds.Remove(target);
    }
}