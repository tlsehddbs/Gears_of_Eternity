using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        // 힐이 필요한 아군이 존재하는지만 확인
        var target = caster.FindLowestHpAlly();
        return target != null && target.currentHP < target.stats.health;
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        return caster.FindLowestHpAlly();
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (target == null || !target.IsAlive()) return;        
        target.ReceiveHealing(effect.skillValue);
        Debug.Log($"[InstantHeal] {caster.name} → {target.name} : {effect.skillValue:F1} 회복");
    }
    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
}
public class BuffAttackSkill : ISkillBehavior
{
    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        return FindTarget(caster, effect) != null && caster.CanUseSkill();
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        return caster.FindNearestAlly();
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        target?.ApplyBuff(effect.buffStat, effect.skillValue, effect.skillDuration, effect.isPercent);
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
}

public class DebuffAttackSkill : ISkillBehavior
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
        target?.ApplyDebuff(effect.buffStat, effect.skillValue, effect.skillDuration);
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
}


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

//기동 중장기병 
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
//증기 돌격병 
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

//급속 파열기
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
//기계 난도자 
public class DoubleAttackSkill : ISkillBehavior
{
    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        return true; // 지속형 패시브는 항상 적용됨
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        return null; 
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

    public void Remove(UnitCombatFSM caster, SkillEffect effect)
    {
        // 이후 상태 해제 시 제거할 수 있도록 구조화
        caster.OnPostAttack = null;
    }

}

//강화 전술기어 
public class GrowBuffOverTimeSkill : ISkillBehavior
{
    private static readonly Dictionary<(UnitCombatFSM, BuffStat), Coroutine> routines = new();

    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect) => true;

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect) => null;

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        var key = (caster, effect.buffStat);
        if (routines.ContainsKey(key)) return;

        var routine = caster.StartCoroutine(GrowRoutine(caster, effect));
        routines[key] = routine;
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect)
    {
        var key = (caster, effect.buffStat);
        if (routines.TryGetValue(key, out var routine))
        {
            caster.StopCoroutine(routine);
            routines.Remove(key);
        }

        float total = effect.skillValue * effect.skillMaxStack;
        caster.ApplyBuff(effect.buffStat, total, 0f, effect.isPercent); // 해제
    }

    private IEnumerator GrowRoutine(UnitCombatFSM caster, SkillEffect effect)
    {
        int stack = 0;

        while (stack < effect.skillMaxStack)
        {
            yield return new WaitForSeconds(effect.skillDelayTime);
            stack++;
            caster.ApplyBuff(effect.buffStat, effect.skillValue, -1f, effect.isPercent);
            Debug.Log($"[GrowBuff] {caster.name} {effect.buffStat} {stack}중첩 (+{effect.skillValue * 100f * stack:F1}%)");
        }
    }
}
//데미지반사 / 기계 반격기
public class ReflectDamageSkill : ISkillBehavior
{
    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect) => true;

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect) => caster;
    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        float percent = effect.skillValue;
        float duration = effect.skillDuration;

        caster.StartCoroutine(ApplyReflect(caster, percent, duration));
    }
    public void Remove(UnitCombatFSM caster, SkillEffect effect)
    {
        caster.OnReflectDamage = null; // 리셋
    }

    private IEnumerator ApplyReflect(UnitCombatFSM caster, float percent, float duration)
    {
        Debug.Log($"[Reflect] 시작 - {caster.name} {duration}s 동안 {percent * 100}% 반사");
        caster.OnReflectDamage = (float damageTaken, UnitCombatFSM attacker) =>
        {
            float reflectDmg = damageTaken * percent;
            attacker?.TakeDamage(reflectDmg);
            Debug.Log($"[반사] {caster.name} → {attacker.name}에게 {reflectDmg:F1} 반사");
        };

        yield return new WaitForSeconds(duration);
        Debug.Log("반사종료");
        caster.OnReflectDamage = null;
    }
}

// 방어력 공유 스킬 //기어 공명기 
public class PassiveAreaBuffSkill : ISkillBehavior
{
    private class BuffRecord
    {
        public BuffStat stat;
        public float value;
    }

    private static readonly Dictionary<UnitCombatFSM, Dictionary<UnitCombatFSM, BuffRecord>> sharedBuffMap = new();

    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect) => true;

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect) => null; // 전방위 버프

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (!sharedBuffMap.ContainsKey(caster))
        {
            sharedBuffMap[caster] = new Dictionary<UnitCombatFSM, BuffRecord>();
            caster.StartCoroutine(BuffLoop(caster, effect));
        }
    }

    private IEnumerator BuffLoop(UnitCombatFSM caster, SkillEffect effect)
    {
        var map = sharedBuffMap[caster];
        float radius = effect.skillRange > 0 ? effect.skillRange : caster.stats.attackDistance * 2f;
        BuffStat stat = effect.buffStat;
        float value = GetBaseStat(caster.stats, stat) * effect.skillValue;

        while (true)
        {
            if (!caster.IsAlive())
            {
                Remove(caster, effect);
                yield break;
            }

            var allAllies = GameObject.FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None)
                .Where(u => u.IsAlive() && u.unitData.faction == caster.unitData.faction && u != caster);

            HashSet<UnitCombatFSM> validTargets = new();

            foreach (var ally in allAllies)
            {
                float dist = Vector3.Distance(caster.transform.position, ally.transform.position);
                if (dist <= radius)
                {
                    validTargets.Add(ally);

                    if (!map.ContainsKey(ally))
                    {
                        ally.ModifyStat(stat, value, false, false);
                        map[ally] = new BuffRecord { stat = stat, value = value };
                        Debug.Log($"[AreaBuff] {caster.name} → {ally.name} : {stat} +{value:F2}");
                    }
                }
            }

            var toRemove = map.Keys.Where(u => !validTargets.Contains(u)).ToList();
            foreach (var u in toRemove)
            {
                var record = map[u];
                u.ModifyStat(record.stat, record.value, false, true);
                Debug.Log($"[AreaBuff] {u.name} ← {record.stat} 버프 해제 -{record.value:F2}");
                map.Remove(u);
            }

            yield return new WaitForSeconds(0.5f);
        }
    }
    public void Remove(UnitCombatFSM caster, SkillEffect effect)
    {
        if (!sharedBuffMap.TryGetValue(caster, out var map)) return;

        foreach (var pair in map)
        {
            var unit = pair.Key;
            var record = pair.Value;
            unit.ModifyStat(record.stat, record.value, false, true);
            Debug.Log($"[AreaBuff:Remove] {unit.name} ← {record.stat} 해제 -{record.value:F2}");

        }

        map.Clear();
        sharedBuffMap.Remove(caster);
    }

    private float GetBaseStat(RuntimeUnitStats stats, BuffStat stat)
    {
        return stat switch
        {
            BuffStat.Attack => stats.attack,
            BuffStat.Defense => stats.defense,
            BuffStat.MoveSpeed => stats.moveSpeed,
            BuffStat.AttackSpeed => stats.attackSpeed,
            BuffStat.AttackDistance => stats.attackDistance,
            BuffStat.DamageReduction => 1f,
            _ => 0f,
        };
    }
}

//공속 증가 스킬 //자동 발사기 

public class AttackSpeedUpSkill : ISkillBehavior
{
    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        return true;
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        return caster;
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        caster.ApplyBuff(effect.buffStat, effect.skillValue, effect.skillDuration, effect.isPercent);
        Debug.Log($"[공속 버프] {caster.name} → +{effect.skillValue * 100f}% / {effect.skillDuration}초");
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
}

//지정 대상에게 치명타 + 공격력 +60%의 피해 //초정밀 저격수
public class CriticalStrikeSkill : ISkillBehavior
{
    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        return caster.CanUseSkill() && caster.FindNearestEnemy() != null;
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        return caster.FindNearestEnemy();
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        float atk = caster.stats.attack;
        // 치명타 배율 적용
        float critDamage = atk * caster.criticalMultiplier;
        // 추가 피해 
        float bonusDamage = atk * effect.skillValue;
        float totalDamage = critDamage + bonusDamage;

        target.TakeDamage(totalDamage, caster);
        Debug.Log($"[CriticalStrike] {caster.name} → {target.name}: 치명타({critDamage:F1}) + 추가({bonusDamage:F1}) = {totalDamage:F1} 피해");

        // 스킬 사용 후 쿨다운 초기화
        caster.skillTimer = 0f;
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
}

// 체력 최상위 적에 표식 → 폭발 // 열선 추적자
public class HeatReactiveMarkSkill : ISkillBehavior
{
    private const float MarkDuration = 6f;
    private const float DamageAmp = 0.15f;    // +15%
    private const float ExplosionRatio = 1f;  // 폭발 시 본체 100%
    private const float AoERatio = 0.5f;      // 주변 50%
    private const float AoERadius = 1.5f;
    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        // 쿨다운 끝났고, 적이 하나 이상 있을 때
        return caster.CanUseSkill() && FindTarget(caster, effect) != null;
    }
    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        // 사거리 안의 적 중 currentHP 최고인 대상
        return caster.FindEnemiesInRange(effect.skillRange)
                     .OrderByDescending(e => e.currentHP)
                     .FirstOrDefault();
    }
}




// 스킬 부가 효과들


//침묵 스킬 //기계 교란수
public class SilenceSkill : ISkillBehavior
{
    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        var enemies = caster.FindEnemiesInRange(effect.skillRange);
        return enemies.Count > 0;
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        return caster.FindNearestEnemy();
    }


    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        var enemies = caster.FindEnemiesInRange(effect.skillRange);
        foreach (var enemy in enemies)
        {
            SilenceSystem.ApplySilence(enemy, effect.skillDuration);
            Debug.Log($"[Silence] {enemy.name} 침묵 상태 적용 ({effect.skillDuration}s)");
        }
    }
    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
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
//침묵 로직 
public static class SilenceSystem
{
    private static readonly Dictionary<UnitCombatFSM, Coroutine> activeSilences = new();

    public static void ApplySilence(UnitCombatFSM target, float duration)
    {
        if (!target.IsAlive()) return;

        if (activeSilences.TryGetValue(target, out var existing))
            target.StopCoroutine(existing);

        target.isSilenced = true;
        var routine = target.StartCoroutine(SilenceRoutine(target, duration));
        activeSilences[target] = routine;
    }

    private static IEnumerator SilenceRoutine(UnitCombatFSM target, float duration)
    {
        yield return new WaitForSeconds(duration);
        target.isSilenced = false;
        activeSilences.Remove(target);
    }
}