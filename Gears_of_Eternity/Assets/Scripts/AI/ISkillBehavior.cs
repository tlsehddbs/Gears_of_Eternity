using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using UnityEngine.AI;
using System.Security;

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
        float healAmount = target.stats.health * effect.skillValue;

        if (target == null || !target.IsAlive()) return;        
        target.ReceiveHealing(healAmount);
        Debug.Log($"[InstantHeal] {caster.name} → {target.name} : {healAmount:F1} 회복");
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
        if (target != null)
        UnitCombatFSM.UnitCombatFSM_DebuffRegistry.ApplyStatDebuffTracked(target, effect.buffStat, effect.skillValue, effect.skillDuration, effect.isPercent);
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
}


// 연속 타격 스킬 / 터빈 절삭자 
// 연속 타격 스킬 / 터빈 절삭자
// 요구사항:
// - 스킬 사거리(effect.skillRange)는 사용하지 않음
// - 평타 거리(현재 프로젝트 기준 = agent.stoppingDistance) 안에서만 발동
// - 데미지는 즉시 적용(연속 타격은 코루틴으로 3회)
// - TakeDamage에 attacker(caster)를 넘겨서 피흡/킬크레딧 등 후속 시스템과 호환
public class MultiHitSkill : ISkillBehavior
{
    // AttackState에서 쓰는 판정과 동일하게 여유값
    private const float RangePadding = 0.05f;

    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null) return false;
        if (!caster.CanUseSkill()) return false;

        // 안전 가드(중복이지만 방어적으로)
        if (caster.IsStunned() || caster.isSilenced) return false;

        var target = FindTarget(caster, effect);
        if (target == null || !target.IsAlive()) return false;

        // 평타거리 체크(스킬 사거리 안 씀)
        return IsWithinBasicAttackRange(caster, target);
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        // 이미 전투 중인 타겟이 있으면 그 타겟 우선
        if (caster.targetEnemy != null && caster.targetEnemy.IsAlive())
            return caster.targetEnemy;

        return caster.FindNearestEnemy();
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null || target == null) return;
        if (!target.IsAlive()) return;

        // Execute는 혹시라도 외부에서 바로 호출될 수 있으니 한 번 더 거리 체크
        if (!IsWithinBasicAttackRange(caster, target))
            return;

        caster.StartCoroutine(MultiHitRoutine(caster, target, effect));
    }

    private static bool IsWithinBasicAttackRange(UnitCombatFSM caster, UnitCombatFSM target)
    {
        // 현재 프로젝트에서 공격 사거리 판정은 agent.stoppingDistance 기반
        float dist = Vector3.Distance(caster.transform.position, target.transform.position);
        return dist <= (caster.agent.stoppingDistance + RangePadding);
    }

    private static IEnumerator MultiHitRoutine(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        // 기존과 동일한 기본값
        int hitCount = 3;
        float damagePercent = (effect != null) ? effect.skillValue : 0.6f;
        float delay = 0.2f;

        for (int i = 0; i < hitCount; i++)
        {
            if (caster == null || !caster.IsAlive()) yield break;
            if (target == null || !target.IsAlive()) yield break;

            // 스킬 도중 타겟이 멀어지면 중단(평타거리 유지 조건)
            if (!IsWithinBasicAttackRange(caster, target))
                yield break;

            float damage = caster.stats.attack * damagePercent;

            // attacker 전달(피흡/처치 트리거/로그 등 호환)
            target.TakeDamage(damage, caster);

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
        if (caster == null) return false;
        if (!caster.CanUseSkill()) return false;

        // 타겟이 없으면 스킬 시도 자체를 막음 (range 체크는 SkillExecutor 쪽에서도 수행됨):contentReference[oaicite:2]{index=2}
        return caster.FindNearestEnemy() != null;
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        return caster.FindNearestEnemy();
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null || target == null) return;
        caster.StartCoroutine(DashAndAttackRoutine(caster, target, effect));
    }

    private IEnumerator DashAndAttackRoutine(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        // -----------------------------
        // 1) 돌진 파라미터 (기존 하드코딩 유지하되, effect.skillRange 있으면 재사용)
        // -----------------------------
        float dashDistance = (effect.skillRange > 0f) ? effect.skillRange : 30f;
        float dashSpeed = 60f;

        Vector3 startPos = caster.transform.position;

        // "전방"을 caster.forward로 고정하면, 타겟을 보고 있지 않을 때 옆으로 돌진할 수 있음
        // 타겟 방향으로 돌진하도록 보정 (XZ 기준)
        Vector3 dir = target.transform.position - startPos;
        dir.y = 0f;

        Vector3 dashDir = (dir.sqrMagnitude > 0.0001f) ? dir.normalized : caster.transform.forward;

        Vector3 endPos = startPos + dashDir * dashDistance;

        // -----------------------------
        // 2) NavMeshAgent 사용 중이면, 돌진 동안 Transform 이동과 충돌할 수 있어서 최소한으로 정지/경로 리셋
        //    (프로젝트에 caster.agent 사용 패턴이 이미 존재):contentReference[oaicite:3]{index=3}
        // -----------------------------
        var agent = caster.agent;
        bool hadAgent = (agent != null && agent.enabled);

        bool prevUpdatePos = false;
        bool prevUpdateRot = false;
        bool prevStopped = false;

        if (hadAgent)
        {
            prevUpdatePos = agent.updatePosition;
            prevUpdateRot = agent.updateRotation;
            prevStopped = agent.isStopped;

            agent.isStopped = true;
            agent.ResetPath();
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        // -----------------------------
        // 3) 돌진 이동 (Lerp는 startPos 고정으로)
        // -----------------------------
        float dashTime = dashDistance / Mathf.Max(0.01f, dashSpeed);
        float t = 0f;

        Quaternion lookRot = Quaternion.LookRotation(dashDir, Vector3.up);

        while (t < dashTime)
        {
            float alpha = t / dashTime;

            caster.transform.position = Vector3.Lerp(startPos, endPos, alpha);
            caster.transform.rotation = lookRot;

            t += Time.deltaTime;
            yield return null;
        }

        caster.transform.position = endPos;
        caster.transform.rotation = lookRot;

        // -----------------------------
        // 4) 히트 판정: "돌진 경로 전체"를 박스로 커버
        //    center = (start+end)/2
        //    halfExtents.z = dashDistance/2
        // -----------------------------
        Vector3 center = (startPos + endPos) * 0.5f;
        // 높이 축은 약간 넉넉하게 잡아서 지형/키 차이로 누락되는 걸 줄임
        Vector3 halfExtents = new Vector3(3.0f, 2.0f, dashDistance * 0.5f);

        Collider[] hits = Physics.OverlapBox(center, halfExtents, lookRot, ~0);

        // 유닛에 콜라이더가 여러 개 붙어있을 수 있으니 중복 타격 방지
        HashSet<UnitCombatFSM> hitUnits = new HashSet<UnitCombatFSM>();

        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (col == null) continue;

            // 핵심 수정 1) 자식 콜라이더 대응
            var enemy = col.GetComponentInParent<UnitCombatFSM>();
            if (enemy == null) continue;

            if (enemy == caster) continue;
            if (!enemy.IsAlive()) continue;
            if (enemy.unitData == null || caster.unitData == null) continue;
            if (enemy.unitData.faction == caster.unitData.faction) continue;

            if (!hitUnits.Add(enemy)) continue;

            float damage = caster.stats.attack * effect.skillValue;

            // 핵심 수정 2) 공격자 전달 (스킬/패시브/로그/UI 일관성)
            enemy.TakeDamage(damage, caster);
        }

        // -----------------------------
        // 5) 가드 스택 부여 (기존 로직 유지)
        // -----------------------------
        int addGuard = Mathf.Max(0, Mathf.RoundToInt(effect.skillMaxStack));
        caster.stats.guardCount += addGuard;

        // -----------------------------
        // 6) NavMeshAgent 복구
        // -----------------------------
        if (hadAgent)
        {
            // 내부 위치 싱크(스냅/튐 방지)
            agent.nextPosition = caster.transform.position;

            agent.updatePosition = prevUpdatePos;
            agent.updateRotation = prevUpdateRot;
            agent.isStopped = prevStopped;
        }
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

//포자 분열사
public class ConeTripleHitSkill : ISkillBehavior
{
    // private const int hitCount = 3;
    // private const float hitDelay = 0.25f;
    // private const float Angle = 90;
    // private const float RangeMultiplier = 10f;

    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        var list = caster.FindEnemiesInCone(effect.skillAngle, effect.skillRange);
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

        float hitCount = effect.skillMaxStack;
        float hitDelay = 0.25f;
        float Angle = effect.skillAngle;
        float RangeMultiplier = effect.skillRange;
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
//군체 연사핵
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

//공속 증가 스킬 //자동 발사기 // 분비 연사체

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

// 체력 최상위 적에 표식 → 폭발 // 화력 관제사
public class HeatReactiveMarkSkill : ISkillBehavior
{
    private const float MarkDuration = 6f;
    private const float DamageAmp = 0.15f;    // +15%
    private const float ExplosionRatio = 1f;  // 폭발 시 본체 100%
    private const float ExplosionAoE = 0.5f;      // 주변 50%
    private const float AoERadius = 8f;

    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null || effect == null) return false;
        if (!caster.CanUseSkill()) return false;

        // 사거리 내 대상 존재 여부로 트리거 판단
        return FindTarget(caster, effect) != null;
    }
    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        // 사거리 안의 적 중 currentHP 최고인 대상
        return caster.FindEnemiesInRange(effect.skillRange)
                     .OrderByDescending(e => e.currentHP)
                     .FirstOrDefault();
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null || target == null || !target.IsAlive()) return;
        //스킬 쿨다운 리셋
        caster.skillTimer = 0;
        //표식 및 폭발 코루틴 시작
        caster.StartCoroutine(ApplyMarkAndExplode(caster, target, effect));
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }

    private IEnumerator ApplyMarkAndExplode(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {

        //표식 적용
        void Amplify(ref float dmg, UnitCombatFSM atk) { dmg *= (1f + DamageAmp); }
        if (target != null)
            target.OnBeforeTakeDamage += Amplify;

        Debug.Log($"[HeatMark] {target.name} 표식 시작 ({MarkDuration}s, 피해 +{DamageAmp * 100f}%)");


        //표식 해제
        try
        {
            yield return new WaitForSeconds(MarkDuration);
        }
        finally
        {
            if (target != null)
                target.OnBeforeTakeDamage -= Amplify; //변경: 누수/영구버프 방지
        }
        // try / finally //try 블록:이 구간을 실행해보고 finally 블록:어떻게 끝나든(정상 종료, return, break, throw 예외) 마지막에 무조건 이 정리 코드를 실행해라
        // 정리코드를 반드시 실행시키는 안전 장치 역할

        if (caster == null || target == null || !target.IsAlive())
            yield break;

        Debug.Log($"[HeatMark] {target.name} 표식 종료 → 폭발");

        //폭발: 본체 고정 피해(100%)
        float baseAtk = caster.stats.attack;
        target.TakeDamage(baseAtk * ExplosionRatio, caster);
        Debug.Log($"[HeatMark] {target.name} 본체 폭발: {baseAtk * ExplosionRatio:F1} 피해");

        //폭발: 주변 적에게 데미지
        ApplyAoEAroundTarget(caster, target, baseAtk, AoERadius); // 본체 제외 로직 포함

        // var others = FindAllEnemies(caster).Where(e => e.IsAlive() && Vector3.Distance(e.transform.position, target.transform.position) <= AoERadius);

        // foreach (var enemy in others)
        // {
        //     enemy.TakeDamage(baseAtk * AoERatio, caster);
        //     Debug.Log($"[HeatMark] {enemy.name} 주변 폭발: {baseAtk * AoERatio:F1} 피해");
        // }
    }

    private void ApplyAoEAroundTarget(UnitCombatFSM caster, UnitCombatFSM center, float baseAtk, float radius)
    {
        if (caster == null || center == null) return;

        // refUnit은 'caster'를 넣어야 적군만 반환
        // (target을 넣으면 target의 적 = caster 편 = 아군이 리턴되는 설계가 되니 주의)
        var enemies = FindAllEnemies(caster);

        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.IsAlive()) continue;

            // 본체는 AoE 대상에서 제외 (100% 피해를 이미 받았기 때문)
            if (enemy == center) continue;

            // 중심거리 판정 (제곱 비교로 sqrt 비용 절감)
            float sqrDist = (enemy.transform.position - center.transform.position).sqrMagnitude;
            if (sqrDist <= radius * radius)
            {
                enemy.TakeDamage(baseAtk * ExplosionAoE, caster);
                Debug.Log($"[HeatMark AoE] {enemy.name} 주변 폭발: {(baseAtk * ExplosionAoE):F1} 피해");
            }
        }
    }

    private static IEnumerable<UnitCombatFSM> FindAllEnemies(UnitCombatFSM refUnit)
    {
        return GameObject.FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None).Where(u => u.IsAlive() && u.unitData.faction != refUnit.unitData.faction);
    }

}

//가장 가까운 적 150% 피해 + 명중 시 5초간 이동속도 30% 감소 //스팀 저격병
public class HeavyStrikeAndSlowSkill : ISkillBehavior
{
    private const float SlowPercent = -0.30f;    // -30% (음수)
    private const float SlowDuration = 5f;       // 5초

    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null || effect == null) return false;
        if (!caster.CanUseSkill()) return false;

        return FindTarget(caster, effect) != null;
    }
    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null || effect == null) return null;

        // 사거리 0이면 의미가 없으니, 반드시 에셋에서 유효 사거리 설정 필요
        var enemies = caster.FindEnemiesInRange(effect.skillRange);
        if (enemies == null || enemies.Count == 0) return null;

        // 가장 가까운 적
        return enemies
            .OrderBy(e => Vector3.SqrMagnitude(e.transform.position - caster.transform.position)) // OrderBy(제곱거리) → 제곱거리가 작은 순으로 전체 목록을 정렬(O(n log n)) //Vector3.SqrMagnitude(Δ) → Δ의 길이의 제곱(제곱거리).
            .FirstOrDefault();
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null || target == null || !target.IsAlive()) return;

        //피해 적용(150%)
        float dmg = caster.stats.attack * effect.skillValue;
        target.TakeDamage(dmg, caster);
        Debug.Log($"[HeavyStrikeAndSlow] {caster.name} → {target.name} : {dmg:F1} (150%)");

        //명중 시 슬로우(이동속도 30% 감소, 5초) - 퍼센트 버프로 음수값 전달
        target.ApplyBuff(BuffStat.MoveSpeed, SlowPercent, SlowDuration, isPercent: true);
        Debug.Log($"[HeavyStrikeAndSlow] {target.name} : MoveSpeed {SlowPercent * -100f:F0}% ↓ ({SlowDuration:F1}s)");
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect){}
}

// 맵 전역: 가장 먼 적에게 1초 간격 원형 AoE 2회(각 80%) // 연산 포격수
public class FarthestDoubleAoeSkill : ISkillBehavior
{
    //데미지는 SKillValue, 원형 범위는 SkillRange
    private const float AoERadius = 8.0f;
    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null || effect == null) return false;
        if (!caster.CanUseSkill()) return false;

        return FindTarget(caster, effect) != null;
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        // 맵 전체에서 가장 먼 적
        return TargetingUtil.FindFarthestEnemyGlobal(caster, aliveOnly: true, xzOnly: true);
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null || target == null || !target.IsAlive()) return;

        caster.StartCoroutine(DoDoubleAoe(caster, target, effect));
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }

    private IEnumerator DoDoubleAoe(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        for (int i = 0; i < effect.skillMaxStack; i++)
        {
            //가장 먼 적에게 n회 날림, 최초 타깃 기준 죽었으면 재탐색
            var firstTarget = (target != null && target.IsAlive()) ? target : FindTarget(caster, effect);

            if (firstTarget == null) yield break;

            // 타격 시점의 타깃 현재 위치를 중심으로 원형AoE
            Vector3 center = firstTarget.transform.position;
            float damage = caster.stats.attack * effect.skillValue;

            ApplyAoeDamage(caster, center, AoERadius, damage);

            yield return new WaitForSeconds(effect.skillDelayTime);
        }
    }

    private void ApplyAoeDamage(UnitCombatFSM caster, Vector3 center, float radius, float damage)
    {
        var cols = Physics.OverlapSphere(center, radius, ~0);
        var hit = new HashSet<UnitCombatFSM>();

        if (cols != null && cols.Length > 0)
        {
            foreach (var col in cols)
            {
                var enemy = col.GetComponentInParent<UnitCombatFSM>();
                if (!IsValidEnemy(enemy, caster)) continue;

                if ((enemy.transform.position - center).sqrMagnitude <= radius * radius)
                {
                    if (hit.Add(enemy))
                    {
                        enemy.TakeDamage(damage, caster);
                        Debug.Log($"[FarthestDoubleAoe] {enemy.name} AoE {damage:F1}");
                    }
                }
            }
        }
        else
        {
            // 콜라이더/레이어 문제 시 폴백
            var all = GameObject.FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None);
            foreach (var enemy in all)
            {
                if (!IsValidEnemy(enemy, caster)) continue;
                if ((enemy.transform.position - center).sqrMagnitude <= radius * radius)
                {
                    if (hit.Add(enemy))
                    {
                        enemy.TakeDamage(damage, caster);
                        Debug.Log($"[FarthestDoubleAoe-Fallback] {enemy.name} AoE {damage:F1}");
                    }
                }
            }
        }
    }

    private static bool IsValidEnemy(UnitCombatFSM u, UnitCombatFSM caster)
    {
        return u != null && u.IsAlive() && u.unitData.faction != caster.unitData.faction;
    }
}


// 고정 포탑: 3초마다 근접 적 AoE(280%) + 사망 시 자폭(400%)  //독성 첨두군주
public class PassiveTurretBarrageSkill : ISkillBehavior
{
    private const float FireIntervalSec = 3.0f; // 3초마다 발사
    private const float DeathMultiplier = 4.0f;  // 자폭 데미지
    private const float DefaultRadius = 9f;  // AoE 기본 반경
    private const float DefaultDeathRad = 10.0f;  // 자폭 기본 반경

    private static readonly Dictionary<UnitCombatFSM, Coroutine> running = new();
    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null || !caster.IsAlive()) return false;
        return !running.ContainsKey(caster);
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect) => caster;

    public void Execute(UnitCombatFSM caster, UnitCombatFSM _, SkillEffect effect)
    {
        if (caster == null || !caster.IsAlive()) return;
        if (running.ContainsKey(caster)) return; // 중복 방지

        caster.disableBasicAttack = true;  //평타 금지 
        if (caster.agent != null) caster.agent.isStopped = true; //이동도 정지

        var co = caster.StartCoroutine(FireLoop(caster, effect));
        running[caster] = co;
    }

    // 패시브 제거 없음
    public void Remove(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null) return;

        // ★ OnDeath → Remove 호출 시점에 자폭 실행(파괴되기 전)
        //    혹시 다른 경로로 Remove가 불릴 수 있으면 '사망일 때만' 자폭
        if (!caster.IsAlive())
        {
            SelfDestruct(caster, DefaultDeathRad);
        }

        // 코루틴 정리
        if (running.TryGetValue(caster, out var co))
        {
            caster.StopCoroutine(co);
            running.Remove(caster);
        }
    }

    // === 메인 루프: 3초마다 근접 적에게 원형 AoE ===
    private IEnumerator FireLoop(UnitCombatFSM caster, SkillEffect effect)
    {
        float fireTimer = 0f;

        while (caster != null)
        {
            // 사망 감지 → 즉시 자폭하고 종료
            if (!caster.IsAlive())
                break;

            // 3초마다 발사
                fireTimer += Time.deltaTime;
            if (fireTimer >= FireIntervalSec)
            {
                fireTimer = 0f;
                FireOnce(caster, effect, DefaultRadius);
            }

            // 이동 불가 보장(안전장치): 에이전트가 있으면 정지
            if (caster.agent != null && !caster.agent.isStopped)
                caster.agent.isStopped = true;

            yield return null;
        }

        // 정리
        if (running.ContainsKey(caster)) running.Remove(caster);
    }

    // 1회 발사: 가장 가까운 적의 현재 위치에 
    private void FireOnce(UnitCombatFSM caster, SkillEffect effect, float radius)
    {
        if (caster == null || !caster.IsAlive()) return;

        // 맵 전역에서 가장 가까운 적
        var target = TargetingUtil.FindNearestEnemyGlobal(caster, aliveOnly: true, xzOnly: true);
        if (target == null) return;

        Vector3 center = target.transform.position;
        float damage = caster.stats.attack * effect.skillValue;

        ApplyAoeDamage(caster, center, radius, damage);
        Debug.Log($"[PassiveTurret] {caster.name} → {target.name} AoE {damage:F1} (r={radius:F1})");
    }

    // 자폭: 자기 중심 원형 AoE로 400% 피해
    private void SelfDestruct(UnitCombatFSM caster, float radius)
    {
        if (caster == null) return;
        float damage = caster.stats.attack * DeathMultiplier;
        Vector3 center = caster.transform.position;

        ApplyAoeDamage(caster, center, radius, damage);
        Debug.Log($"[PassiveTurret] {caster.name} Self-Destruct AoE {damage:F1} (r={radius:F1})");
    }


    // 원형 AoE 공통 처리 (OverlapSphere → HashSet 중복 방지)
    private static void ApplyAoeDamage(UnitCombatFSM caster, Vector3 center, float radius, float damage)
    {
        var cols = Physics.OverlapSphere(center, radius, ~0);
        var hit = new HashSet<UnitCombatFSM>();

        if (cols != null && cols.Length > 0)
        {
            foreach (var col in cols)
            {
                var enemy = col.GetComponentInParent<UnitCombatFSM>();
                if (!IsValidEnemy(enemy, caster)) continue;

                // 콜라이더 오차 보정(중심 간 제곱거리 확인)
                if ((enemy.transform.position - center).sqrMagnitude <= radius * radius)
                {
                    if (hit.Add(enemy))
                        enemy.TakeDamage(damage, caster);
                }
            }
        }
        else
        {
            // 폴백: 전 유닛 스캔(성능↓)
            var all = GameObject.FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None);
            foreach (var enemy in all)
            {
                if (!IsValidEnemy(enemy, caster)) continue;
                if ((enemy.transform.position - center).sqrMagnitude <= radius * radius)
                {
                    if (hit.Add(enemy))
                        enemy.TakeDamage(damage, caster);
                }
            }
        }
    }
    private static bool IsValidEnemy(UnitCombatFSM u, UnitCombatFSM caster)
    {
        return u != null && u.IsAlive() && u.unitData.faction != caster.unitData.faction;
    }
}


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

// 4연속(각 60%), 4타 명중 시 2초 실명, 쿨타임 7초 //연속 발사기
public class QuadFlurryBlindSkill : ISkillBehavior
{
    private const int HitCount = 4;
    private const float TotalWindowSec = 1.0f;    // 1초 안에 4타
    private const float Epsilon = 0.0001f; // 안전용


    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null || effect == null) return false;
        if (!caster.CanUseSkill()) return false;

        return FindTarget(caster, effect) != null;
    }


    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null) return null;
        var enemies = caster.FindEnemiesInRange(effect.skillRange);
        return TargetingUtil.FindNearestFromList(caster, enemies, enemyOnly: true, aliveOnly: true, xzOnly: true);
    }


    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null || target == null || !target.IsAlive()) return;
        caster.StartCoroutine(CoFlurry(caster, target, effect));
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }


    private IEnumerator CoFlurry(UnitCombatFSM caster, UnitCombatFSM initialTarget, SkillEffect effect)
    {
        // 1초 안에 4타를 누적: 간격 1/3초씩 3번 대기하면 0s, 0.333s, 0.666s, 0.999s 타격
        float waitBetween = TotalWindowSec / (HitCount - 1 + Epsilon);

        UnitCombatFSM target = initialTarget;
        for (int i = 0; i < HitCount; i++)
        {
            // 타격 순간마다 대상이 죽었으면 중단
            if (caster == null || target == null || !caster.IsAlive() || !target.IsAlive())
                yield break;

            // 단일 타겟 직격
            float damage = caster.stats.attack * effect.skillValue;
            target.TakeDamage(damage, caster);
            Debug.Log($"[QuadFlurryBlind] {caster.name} → {target.name} : hit {i+1}/{HitCount}, {damage:F1}");

            // 마지막 타격: 실명 부여
            if (i == HitCount - 1)
            {
                if (target.blind != null)
                {
                    target.blind.Apply(effect.skillDuration);
                    Debug.Log($"[QuadFlurryBlind] {target.name} BLIND for {effect.skillDuration:F2}s");
                }
            }

            // 다음 타격까지 대기 (마지막 타는 대기 없음)
            if (i < HitCount - 1)
                yield return new WaitForSeconds(waitBetween);
        }
    }
}

//자기 중심 원형 HoT(3s, 0.2tick, 5%/tick) + 방어력 +20%(8s) //방호 조정사
public class HealingAuraDefenseBuffSKill : ISkillBehavior
{
    // 사양: 자기 중심 원형 범위 내 아군 대상
    // - 3초 HoT: 0.2초마다 각 아군 '최대체력 * 5%' 회복
    // - 방어력 +20% (8초 지속) 1회 부여
    // 파라미터 매핑(데이터드리븐):
    // - effect.skillRange     : 반경 (0이면 attackDistance*2로 폴백)
    // - effect.skillDuration  : HoT 총 지속(기본 3.0)
    // - effect.skillDelayTime : HoT 틱 간격(기본 0.2)
    // - effect.skillValue     : HoT 1틱당 회복 비율(예: 0.05 = 5%)
    // - effect.skillMaxStack  : 방어력 버프 비율(예: 0.2 = +20%)
    // - 방어 버프 지속시간은 현재 사양 고정 8초(필요 시 효과 분리/데이터화 권장)
    private const float DefaultHoTDuration = 3f;
    private const float DefaultTick = 0.2f;
    private const float DefaultBuffDuration = 8f;

    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null || !caster.CanUseSkill()) return false;

        float radius = GetRadius(caster, effect);
        // 반경 내 '아군(자기 포함)' 중 회복 여지가 있는 대상이 1명 이상이면 발동
        var all = GameObject.FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            var u = all[i];
            if (u == null || !u.IsAlive()) continue;
            if (u.unitData.faction != caster.unitData.faction) continue;

            float dist = Vector3.Distance(u.transform.position, caster.transform.position);
            if (dist <= radius && u.currentHP < u.stats.health - 0.5f) // 0.5 체력 단위 여유치
                return true;
        }
        return false;
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        // 자기 중심 스킬 → 이동 불필요, 즉시 시전 경로
        return caster;
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null || !caster.IsAlive()) return;

        float radius     = GetRadius(caster, effect);
        float hotDur     = effect.skillDuration  > 0f ? effect.skillDuration  : DefaultHoTDuration;
        float tick       = effect.skillDelayTime > 0f ? effect.skillDelayTime : DefaultTick;
        float healPct    = Mathf.Max(0f, effect.skillValue);      // 0.05 = 5%/tick
        float defPct     = Mathf.Max(0f, effect.skillAngle);   // 0.20 = +20%
        float buffDurSec = DefaultBuffDuration;                    // 사양 고정 8초

        // 1) 방어버프: 시전 시점 반경 내 아군에게 1회 부여(8초)
        var alliesAtCast = FindAlliesInRadius(caster, radius, includeSelf: true);
        foreach (var ally in alliesAtCast)
            ally.ApplyBuff(BuffStat.Defense, defPct, buffDurSec, isPercent: true);

        // 2) 3초 HoT: 0.2초 간격으로 반경 내 아군을 스캔하여 치유
        caster.StartCoroutine(CoHealAura(caster, radius, hotDur, tick, healPct));

        // 쿨다운 초기화
        caster.skillTimer = 0f;
        Debug.Log($"[HealingAuraDefenseBuff] {caster.name} : HoT {hotDur:F1}s @ {tick:F2}s, {healPct*100f:F1}%/tick, DEF +{defPct*100f:F0}% for {buffDurSec:F1}s");
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect) { /* 일시효과, Remove 불필요 */ }

    private IEnumerator CoHealAura(UnitCombatFSM caster, float radius, float hotDuration, float tick, float healPct)
    {
        float t = 0f;
        while (caster != null && caster.IsAlive() && t < hotDuration)
        {
            var allies = FindAlliesInRadius(caster, radius, includeSelf: true);
            for (int i = 0; i < allies.Count; i++)
            {
                var ally = allies[i];
                if (ally == null || !ally.IsAlive()) continue;

                float amount = ally.stats.health * healPct; // 최대체력 기준
                ally.ReceiveHealing(amount);
            }
            yield return new WaitForSeconds(tick);
            t += tick;
        }
    }

    private static List<UnitCombatFSM> FindAlliesInRadius(UnitCombatFSM caster, float radius, bool includeSelf)
    {
        var list = new List<UnitCombatFSM>();
        var all = GameObject.FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            var u = all[i];
            if (u == null || !u.IsAlive()) continue;
            if (!includeSelf && u == caster) continue;
            if (u.unitData.faction != caster.unitData.faction) continue;

            float dist = Vector3.Distance(u.transform.position, caster.transform.position);
            if (dist <= radius) list.Add(u);
        }
        return list;
    }

    private static float GetRadius(UnitCombatFSM caster, SkillEffect effect)
    {
        return (effect.skillRange > 0f) ? effect.skillRange : caster.stats.attackDistance * 2f;
    }
}

/// <summary>
/// 가장 공격력이 높은 아군의 "현재 위치"에 원형 버프지대를 1개 생성.
/// - 지대 지속: effect.skillDuration (기본 7초)
/// - 반경: effect.skillRange (지정 권장; 0이면 caster.attackDistance * 2f로 폴백)
/// - 지대 안 아군 버프: 
///     공격력 + effect.skillValue(예: 0.25 = +25%)
///     공격속도 + effect.skillMaxStack(예: 0.15 = +15%)
///     이동속도 + effect.skillDelayTime(예: 0.15 = +15%)
/// - 쿨타임: SkillData.skillCoolDown = 18
/// - 시각화: LineRenderer 원으로 위치/반경 표시
/// 
/// 주의: 버프 중첩 방지를 위해 '지대 내부에 처음 진입 시 1회 적용' / '지대에서 벗어나면 즉시 해제' 방식으로
///       내부 Dictionary로 추적하여 정확히 되돌린다. (ModifyStat의 isRemove=true 사용)
/// </summary>
public class EmpowerZoneHighestAttackAllySkill : ISkillBehavior
{
    // 트리거 조건: 본 구조에선 SkillExecutor가 쿨/침묵을 먼저 체크하므로 True면 충분.
    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect) => true;

    // 이동 유도 방지: target을 caster로 돌려 "사거리 체크 → 이동" 로직을 우회한다.
    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect) => caster;

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null || !caster.IsAlive()) return;

        // 1) 대상 선정: 같은 진영 중 공격력 최대 유닛 (사망자 제외)
        var all = GameObject.FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None);
        UnitCombatFSM best = null;
        float bestAtk = float.MinValue;

        for (int i = 0; i < all.Length; i++)
        {
            var u = all[i];
            if (u == null || !u.IsAlive()) continue;
            if (u.unitData.faction != caster.unitData.faction) continue;

            if (u.stats.attack > bestAtk)
            {
                bestAtk = u.stats.attack;
                best = u;
            }
        }

        // 아군이 없으면 취소
        if (best == null) return;

        // 2) 파라미터 해석
        float zoneDuration = effect.skillDuration > 0f ? effect.skillDuration : 7f;
        float radius = effect.skillRange > 0f ? effect.skillRange : caster.stats.attackDistance * 2f;

        // 퍼센트 (0.25=+25%, 0.15=+15%)
        float atkPct      = Mathf.Max(0f, effect.skillValue);       // 권장 0.25
        float asPct       = Mathf.Max(0f, effect.skillMaxStack);    // 권장 0.15
        float msPct       = Mathf.Max(0f, effect.skillDelayTime);   // 권장 0.15

        Vector3 center = best.transform.position;

        // 3) 지대 생성
        var zoneGO = new GameObject($"BuffZone_{caster.name}");
        zoneGO.transform.position = center;

        var zone = zoneGO.AddComponent<BuffZoneController>();
        zone.Initialize(caster, radius, zoneDuration, atkPct, asPct, msPct);

        // 4) 쿨다운 초기화
        caster.skillTimer = 0f;

        Debug.Log($"[EmpowerZone] {caster.name} → center:{center} r:{radius:F1} dur:{zoneDuration:F1}s atk+{atkPct:P0} as+{asPct:P0} ms+{msPct:P0}");
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect) { /* 지대는 일시 오브젝트로 자동 정리 */ }
}

/// <summary>
/// CleanseAndShieldAoE
/// - 자신 중심 원형 범위 내 아군 대상:
///   1) 해로운 효과 정화(실명/침묵/출혈/스탯 감소 디버프)
///   2) 보호막 부여: 시전자 최대 체력의 12% (duration=6s)
/// - 쿨타임: 16s
/// - 범위: effect.skillRange (0이면 공격 사거리*2로 폴백)
/// </summary>
public class CleanseAndShieldAoESkill : ISkillBehavior
{
    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        // 별도 조건 없이 쿨타임만으로 발동(정화/보호막은 빈 타깃이어도 안전)
        return caster != null && caster.CanUseSkill();
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        // 자기 주변 광역형이므로 이동 유도 방지 위해 caster 반환
        return caster;
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null || !caster.IsAlive()) return;

        // === 파라미터 해석 ===
        float radius = effect.skillRange > 0f ? effect.skillRange : caster.stats.attackDistance * 2f;
        float shieldDuration = effect.skillDuration > 0f ? effect.skillDuration : 6f;  // 명세: 6초
        // 명세: 시전자 최대 체력의 12%
        float shieldAmount = caster.stats.health * 0.12f; // 12%

        Vector3 center = caster.transform.position;

        // === 범위 내 아군 수집 ===
        // 우선 물리 콜라이더로 수집(빠름), 누락 시 폴백으로 전역 스캔
        var hits = Physics.OverlapSphere(center, radius, ~0);
        HashSet<UnitCombatFSM> allies = new();

        if (hits != null && hits.Length > 0)
        {
            foreach (var col in hits)
            {
                var u = col.GetComponentInParent<UnitCombatFSM>();
                if (u == null || !u.IsAlive()) continue;
                if (u.unitData.faction != caster.unitData.faction) continue;

                // 중심 오차 보정(진짜 원 안인지 제곱거리 체크)
                if ((u.transform.position - center).sqrMagnitude <= radius * radius)
                    allies.Add(u);
            }
        }
        else
        {
            var all = GameObject.FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                var u = all[i];
                if (u == null || !u.IsAlive()) continue;
                if (u.unitData.faction != caster.unitData.faction) continue;

                if ((u.transform.position - center).sqrMagnitude <= radius * radius)
                    allies.Add(u);
            }
        }

        // === 정화 + 보호막 ===
        foreach (var ally in allies)
        {
            // 1) 해로운 효과 해제
            CleanseHarmful(ally);

            // 2) 보호막 부여(누적 허용: 기존 barrier 위에 추가)
            ally.ApplyBarrier(shieldAmount, shieldDuration);
        }

        // 쿨다운 리셋은 SkillExecutor가 수행함
        Debug.Log($"[Cleanse+Shield] {caster.name} r={radius:F1}, allies={allies.Count}, shield={shieldAmount:F0}/{shieldDuration:F1}s");
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }

    // ---------------- 내부 유틸 ----------------
    private static void CleanseHarmful(UnitCombatFSM u)
    {
        if (u == null || !u.IsAlive()) return;

        // 침묵: 플래그만 즉시 해제해도 안정적으로 동작(코루틴 만료 시점에 false로 재확인됨)
        u.isSilenced = false; // 침묵 해제

        // 실명: BlindSystem 존재 시 해제 시도
        // BlindSystem의 구체 API가 프로젝트별로 다를 수 있으니,
        // 아래 우선순위로 안전하게 시도:
        // 1) Clear()/Remove() 메서드가 있으면 호출
        // 2) IsBlinded 세터가 공개면 false로
        try
        {
            if (u.blind != null)
            {
                var t = u.blind.GetType();
                var clear = t.GetMethod("Clear", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (clear != null) clear.Invoke(u.blind, null);
                else
                {
                    var remove = t.GetMethod("Remove", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (remove != null) remove.Invoke(u.blind, null);
                    else
                    {
                        var prop = t.GetProperty("IsBlinded");
                        if (prop != null && prop.CanWrite) prop.SetValue(u.blind, false);
                        else
                        {
                            var field = t.GetField("IsBlinded", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            field?.SetValue(u.blind, false);
                        }
                    }
                }
            }
        }
        catch { /* 릴리즈/IL2CPP 환경에서 반사 실패 시 무시 */ }

        // 출혈: BleedSystem에 제거 API 제공(아래 4)에서 구현)
        BleedSystem.RemoveBleed(u);

        // 스탯 감소 디버프: 추적형 디버프를 전부 즉시 해제(아래 5)에서 구현)
        UnitCombatFSM.UnitCombatFSM_DebuffRegistry.CleanseAllStatDebuffs(u);
    }
}

/// <summary>
/// RectStunArmorDown
/// - "가장 가까운 적" 방향으로 직사각형 범위 생성(캐스터 기준 전방 길이 L, 폭 W)
/// - 범위 내 모든 적: 기절 4초 + 방어력 15% 감소 7초
/// - 쿨타임: 14초
/// - 파라미터 매핑:
///     effect.skillRange      = L(길이, m)
///     effect.skillMaxStack   = W(폭, m)
///     effect.skillDuration   = 기절 지속(기본 4)
///     effect.skillDelayTime  = 방어↓ 지속(기본 7)
///     effect.skillValue      = 방어↓ 비율(기본 0.15 = 15%)
/// </summary>
public class RectStunArmorDownSkill : ISkillBehavior
{
    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        return caster != null && TargetingUtil.FindNearestEnemyGlobal(caster, aliveOnly: true, xzOnly: true) != null;
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        // 이동 유도 방지: 즉시 시전형이므로 caster 반환
        return caster;
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null || !caster.IsAlive()) return;

        // ---- 파라미터 ----
        float L = effect.skillRange     > 0f ? effect.skillRange     : caster.stats.attackDistance * 2f;
        float W = effect.skillMaxStack  > 0f ? effect.skillMaxStack  : 4f;   // 폭 기본 4m
        float stunDur   = effect.skillDuration  > 0f ? effect.skillDuration  : 4f;
        float armorDown = effect.skillValue     > 0f ? effect.skillValue     : 0.15f; // 15%
        float armorDur  = effect.skillDelayTime > 0f ? effect.skillDelayTime : 7f;

        var enemy = TargetingUtil.FindNearestEnemyGlobal(caster, aliveOnly: true, xzOnly: true);
        if (enemy == null) return;

        // ---- 직사각형 좌표계 구성 ----
        Vector3 cpos = caster.transform.position;
        Vector3 dir = enemy.transform.position - cpos;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = caster.transform.forward;
        else dir.Normalize();

        Vector3 right = new Vector3(dir.z, 0f, -dir.x); // 시계방향 직교벡터
        Vector3 center = cpos + dir * (L * 0.5f);       // 캐스터 앞쪽으로 L/2

        float halfW = W * 0.5f;

        // ---- 디버그 표시 ----
        RectAoeDebug.Spawn(center, dir, L, W, 0.6f);

        // ---- 대상 판정 & 적용 ----
        var all = GameObject.FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None);
        int hitCount = 0;

        for (int i = 0; i < all.Length; i++)
        {
            var u = all[i];
            if (u == null || !u.IsAlive()) continue;
            if (u.unitData.faction == caster.unitData.faction) continue; // 적만

            Vector3 to = u.transform.position - center;
            to.y = 0f;

            // 로컬 좌표 투영
            float x = Vector3.Dot(to, right);
            float z = Vector3.Dot(u.transform.position - cpos, dir); // 캐스터 기준 전방 거리

            bool inside = Mathf.Abs(x) <= halfW && z >= 0f && z <= L;
            if (!inside) continue;

            // 1) 기절
            StunSystem.Apply(u, stunDur);

            // 2) 방어력 15% 감소(정화 호환을 위해 추적형으로 적용)
            UnitCombatFSM.UnitCombatFSM_DebuffRegistry.ApplyStatDebuffTracked(u, BuffStat.Defense, armorDown, armorDur, isPercent: true);

            hitCount++;
        }

        // 쿨다운 초기화
        caster.skillTimer = 0f;
        Debug.Log($"[RectStunArmorDown] {caster.name} L={L:F1}, W={W:F1}, hit={hitCount}, stun={stunDur:F1}s, armor↓={armorDown:P0}/{armorDur:F1}s");
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
}

//치명타 시 출혈 부여 //광증 난도수 
public class BleedOnCritPassiveSkill : ISkillBehavior
{
    // caster별로 구독한 핸들러를 저장해서 해제할 수 있게
    private static readonly Dictionary<UnitCombatFSM, System.Action> _postAttackHandlers = new();

    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect) => true;
    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect) => null;

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null) return;

        // 같은 패시브가 여러 번 Apply되는 상황(유닛 재스폰/중복 Apply 등) 방지
        if (_postAttackHandlers.ContainsKey(caster)) return;

        // 데이터가 비어있어도 동작하도록 기본값
        float percentPerSec    = (effect != null && effect.skillValue > 0f) ? effect.skillValue : 0.10f;
        float durationPerStack = (effect != null && effect.skillDelayTime > 0f) ? effect.skillDelayTime : 3f;
        int   maxStack         = (effect != null && effect.skillMaxStack > 0f) ? Mathf.RoundToInt(effect.skillMaxStack) : 3;

        System.Action handler = () =>
        {
            // caster가 죽었거나 비활성화된 경우 안전 가드
            if (caster == null || !caster.IsAlive()) return;

            // 직전 평타가 치명타가 아니면 발동하지 않음
            if (!caster.lastAttackWasCritical) return;

            // 평타로 때린 대상(Attack()에서 사용한 targetEnemy)을 기준으로 적용
            var victim = caster.targetEnemy;
            if (victim == null || !victim.IsAlive()) return;

            BleedSystem.ApplyBleed(victim, durationPerStack, maxStack, percentPerSec);
            //Debug.Log($"[BleedOnCritPassive] {caster.name} 치명타 → {victim.name} 출혈 (dur/stack={durationPerStack:F1}s, max={maxStack}, %/s={percentPerSec:P0})");
        };

        caster.OnPostAttack += handler;
        _postAttackHandlers[caster] = handler;
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null) return;

        if (_postAttackHandlers.TryGetValue(caster, out var handler))
        {
            caster.OnPostAttack -= handler;
            _postAttackHandlers.Remove(caster);
        }
    }
}

/// <summary>
/// 패시브: 피흡 + 처치 회복
/// - 피해를 입힌 쪽에서 동작해야 하므로 UnitCombatFSM.OnDealDamage / OnKillEnemy 이벤트를 사용.
/// - SkillEffect 파라미터 약속(권장):
///   effect.skillValue     = 피흡 비율 (0.2 = 20%)
///   effect.skillDelayTime = 킬 힐 비율 (0.15 = 15%, 최대체력 기준)
///   effect.skillDuration  <= 0 (패시브 적용 조건)
/// </summary>
public class LifeStealAndKillHealPassiveSkill : ISkillBehavior
{
    private static readonly Dictionary<UnitCombatFSM, Action<float, UnitCombatFSM>> _dealHandlers = new();
    private static readonly Dictionary<UnitCombatFSM, Action<UnitCombatFSM>> _killHandlers = new();

    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect) => true;
    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect) => null;

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null) return;

        // 중복 구독 방지 (유닛 생성/재적용 상황)
        if (_dealHandlers.ContainsKey(caster) || _killHandlers.ContainsKey(caster))
            return;

        float lifeStealRatio = (effect != null) ? Mathf.Clamp01(effect.skillValue) : 0.20f;
        float killHealRatio  = (effect != null) ? Mathf.Clamp01(effect.skillDelayTime) : 0.15f;

        // 1) 피흡: 내가 입힌 "실제 피해"의 일정 비율만큼 회복
        Action<float, UnitCombatFSM> onDeal = (dealtDamage, victim) =>
        {
            if (caster == null || !caster.IsAlive()) return;
            if (dealtDamage <= 0f) return;

            float heal = dealtDamage * lifeStealRatio;
            if (heal <= 0f) return;

            caster.ReceiveHealing(heal);
            Debug.Log($"[LifeSteal] {caster.name} dealt {dealtDamage:F1} to {victim?.name} → heal {heal:F1} ({lifeStealRatio:P0})");
        };

        // 2) 처치 회복: 내가 적을 처치하면 내 최대체력(stats.health)의 일정 비율 회복
        Action<UnitCombatFSM> onKill = (victim) =>
        {
            if (caster == null || !caster.IsAlive()) return;

            float heal = caster.stats.health * killHealRatio;
            if (heal <= 0f) return;

            caster.ReceiveHealing(heal);
            Debug.Log($"[KillHeal] {caster.name} killed {victim?.name} → heal {heal:F1} ({killHealRatio:P0} of MaxHP)");
        };

        caster.OnDealDamage += onDeal;
        caster.OnKillEnemy  += onKill;

        _dealHandlers[caster] = onDeal;
        _killHandlers[caster] = onKill;
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null) return;

        if (_dealHandlers.TryGetValue(caster, out var onDeal))
        {
            caster.OnDealDamage -= onDeal;
            _dealHandlers.Remove(caster);
        }

        if (_killHandlers.TryGetValue(caster, out var onKill))
        {
            caster.OnKillEnemy -= onKill;
            _killHandlers.Remove(caster);
        }
    }
}


//광기 절단자
/// <summary>
/// 패시브:
/// - 기본 공격(Attack) 성공 후 OnPostAttack마다 스택 +1 (최대 6)
/// - 스택당:
///    공격속도 +5% (주의: attackSpeed가 '공격간격(초)'이므로 -5%로 구현 = 더 빠름)
///    이동속도 +3%
///    받는 피해 +3%
/// - 풀스택(6) 도달 시: 2초 유지 후 스택 초기화 + (공속 -15%) 디버프 4초
/// - 디버프 중에는 스택이 쌓이지 않음
/// </summary>
public class StackingHasteThenExhaustPassiveSkill : ISkillBehavior
{
    private class Rec
    {
        public int stacks;

        // 우리가 실제로 적용한 총합 보너스 (스택 변경 시 이전값 제거 → 새값 적용)
        public float appliedAtkSpeedBonus; // (공격간격 기준) 공속 증가 = 음수
        public float appliedMoveSpeedBonus;

        public bool isHoldingFullStack;
        public bool isExhausted;
        public bool penaltyApplied;

        public Coroutine cycleRoutine;

        public Action postAttackHandler;
        public UnitCombatFSM.BeforeTakeDamageHandler beforeTakeDamageHandler;
    }

    private static readonly Dictionary<UnitCombatFSM, Rec> _map = new();

    private const int   MaxStacks = 6;
    private const float AtkSpeedPerStack = 0.05f;      // +5% (공격간격 기준이면 -5%로 적용)
    private const float MoveSpeedPerStack = 0.03f;     // +3%
    private const float DamageTakenPerStack = 0.03f;   // +3%

    private const float FullStackHoldSeconds = 2f;

    private const float ExhaustAtkSpeedPenalty = 0.15f; // 공속 -15% (공격간격 기준이면 +15%로 적용)
    private const float ExhaustSeconds = 4f;

    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect) => true;
    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect) => null;

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null) return;
        if (_map.ContainsKey(caster)) return; // 중복 구독 방지

        var rec = new Rec();

        // 받는 피해 증가: TakeDamage 계산 후(OnBeforeTakeDamage) 최종 피해에 곱해줌
        rec.beforeTakeDamageHandler = (ref float dmg, UnitCombatFSM attacker) =>
        {
            if (caster == null || !caster.IsAlive()) return;
            if (rec.stacks <= 0) return;

            float mult = 1f + (DamageTakenPerStack * rec.stacks); // 최대 1.18
            dmg *= mult;
        };
        caster.OnBeforeTakeDamage += rec.beforeTakeDamageHandler;

        // 기본 공격 후처리(Attack() 끝에서 호출되는 OnPostAttack)에 반응
        rec.postAttackHandler = () =>
        {
            if (caster == null || !caster.IsAlive()) return;

            // 디버프 중엔 스택 안 쌓임
            if (rec.isExhausted) return;

            // 이미 풀스택 유지 구간이면 추가 스택 없음
            if (rec.isHoldingFullStack) return;

            int next = Mathf.Min(rec.stacks + 1, MaxStacks);
            if (next == rec.stacks) return;

            SetStacks(caster, rec, next);

            // 풀스택 도달 → 사이클 시작(2초 유지 후 초기화 + 디버프)
            if (next >= MaxStacks)
            {
                rec.isHoldingFullStack = true;

                if (rec.cycleRoutine != null)
                    caster.StopCoroutine(rec.cycleRoutine);

                rec.cycleRoutine = caster.StartCoroutine(CoFullStackCycle(caster, rec));
            }
        };
        caster.OnPostAttack += rec.postAttackHandler;

        _map[caster] = rec;
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null) return;
        if (!_map.TryGetValue(caster, out var rec)) return;

        if (rec.cycleRoutine != null)
            caster.StopCoroutine(rec.cycleRoutine);

        // 남아있을 수 있는 버프/디버프 원복
        RemoveStackBuffs(caster, rec);
        RemoveExhaustPenalty(caster, rec);

        // 핸들러 해제
        if (rec.postAttackHandler != null)
            caster.OnPostAttack -= rec.postAttackHandler;

        if (rec.beforeTakeDamageHandler != null)
            caster.OnBeforeTakeDamage -= rec.beforeTakeDamageHandler;

        _map.Remove(caster);
    }

    private static void SetStacks(UnitCombatFSM caster, Rec rec, int newStacks)
    {
        // 이전 스택 보너스 제거
        RemoveStackBuffs(caster, rec);

        rec.stacks = newStacks;

        if (newStacks <= 0)
        {
            rec.appliedAtkSpeedBonus = 0f;
            rec.appliedMoveSpeedBonus = 0f;
            return;
        }

        // 공격간격(초) 기준: 공속 증가 = 간격 감소 => 음수
        rec.appliedAtkSpeedBonus = -(AtkSpeedPerStack * newStacks);   // 최대 -0.30
        rec.appliedMoveSpeedBonus = (MoveSpeedPerStack * newStacks);  // 최대 +0.18

        caster.ModifyStat(BuffStat.AttackSpeed, rec.appliedAtkSpeedBonus, isPercent: true, isRemove: false);
        caster.ModifyStat(BuffStat.MoveSpeed, rec.appliedMoveSpeedBonus, isPercent: true, isRemove: false);
    }

    private static void RemoveStackBuffs(UnitCombatFSM caster, Rec rec)
    {
        if (rec.appliedAtkSpeedBonus != 0f)
            caster.ModifyStat(BuffStat.AttackSpeed, rec.appliedAtkSpeedBonus, isPercent: true, isRemove: true);

        if (rec.appliedMoveSpeedBonus != 0f)
            caster.ModifyStat(BuffStat.MoveSpeed, rec.appliedMoveSpeedBonus, isPercent: true, isRemove: true);

        rec.appliedAtkSpeedBonus = 0f;
        rec.appliedMoveSpeedBonus = 0f;
    }

    private static IEnumerator CoFullStackCycle(UnitCombatFSM caster, Rec rec)
    {
        // 풀스택 2초 유지
        yield return new WaitForSeconds(FullStackHoldSeconds);

        if (caster == null || !caster.IsAlive()) yield break;

        // 스택 초기화
        SetStacks(caster, rec, 0);
        rec.isHoldingFullStack = false;

        // 디버프(공속 -15%) 4초 + 이 동안 스택 금지
        rec.isExhausted = true;
        ApplyExhaustPenalty(caster, rec);

        yield return new WaitForSeconds(ExhaustSeconds);

        if (caster == null || !caster.IsAlive()) yield break;

        RemoveExhaustPenalty(caster, rec);
        rec.isExhausted = false;

        rec.cycleRoutine = null;
    }

    private static void ApplyExhaustPenalty(UnitCombatFSM caster, Rec rec)
    {
        if (rec.penaltyApplied) return;

        // 공격간격(초) 기준: 공속 -15% = 간격 +15% => +0.15
        caster.ModifyStat(BuffStat.AttackSpeed, ExhaustAtkSpeedPenalty, isPercent: true, isRemove: false);
        rec.penaltyApplied = true;
    }

    private static void RemoveExhaustPenalty(UnitCombatFSM caster, Rec rec)
    {
        if (!rec.penaltyApplied) return;

        caster.ModifyStat(BuffStat.AttackSpeed, ExhaustAtkSpeedPenalty, isPercent: true, isRemove: true);
        rec.penaltyApplied = false;
    }
}


// 3겹 얇은 보호막 스킬
public class TripleLayerThinShieldSkill : ISkillBehavior
{
    private class Rec
    {
        public bool shieldActive;
        public int layers;
        public float perLayerAbsorb;

        public float breakDR;       // 0.2
        public float breakDRDuration; // 1.0

        public bool drApplied;
        public Coroutine shieldExpireRoutine;
        public Coroutine drRoutine;

        public UnitCombatFSM.BeforeTakeDamageHandler beforeHandler;
        public bool hooked;
    }

    private static readonly Dictionary<UnitCombatFSM, Rec> _map = new();

    // 기본값(데이터 미입력 시)
    private const float DefaultLayerPercent = 0.12f;
    private const int   DefaultLayers = 3;
    private const float DefaultDuration = 8f;

    private const float DefaultBreakDR = 0.20f;
    private const float DefaultBreakDRDuration = 1f;

    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null) return false;
        if (!caster.CanUseSkill()) return false;

        // 이미 보호막이 활성화 중이면 재시전 방지
        if (_map.TryGetValue(caster, out var rec) && rec.shieldActive)
            return false;

        return true;
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        return caster; // 자기 자신
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null) return;

        if (!_map.TryGetValue(caster, out var rec))
        {
            rec = new Rec();
            _map[caster] = rec;
        }

        // 이미 활성화면 중복 실행 방지
        if (rec.shieldActive) return;

        float layerPct = (effect != null && effect.skillValue > 0f) ? effect.skillValue : DefaultLayerPercent;
        int layers     = (effect != null && effect.skillMaxStack > 0f) ? Mathf.RoundToInt(effect.skillMaxStack) : DefaultLayers;
        float duration = (effect != null && effect.skillDuration > 0f) ? effect.skillDuration : DefaultDuration;

        rec.breakDR = DefaultBreakDR;                 // 고정: 20%
        rec.breakDRDuration = DefaultBreakDRDuration; // 고정: 1초

        rec.shieldActive = true;
        rec.layers = Mathf.Max(1, layers);
        rec.perLayerAbsorb = caster.stats.health * layerPct; // 시전 시점 MaxHP 기준으로 고정

        // 혹시 이전 루틴이 남아있으면 정리
        if (rec.shieldExpireRoutine != null) caster.StopCoroutine(rec.shieldExpireRoutine);
        rec.shieldExpireRoutine = caster.StartCoroutine(CoExpireShield(caster, rec, duration));

        // OnBeforeTakeDamage 훅 등록(중복 등록 방지)
        if (!rec.hooked)
        {
            rec.beforeHandler = (ref float dmg, UnitCombatFSM attacker) =>
            {
                if (caster == null || !caster.IsAlive()) return;
                if (!rec.shieldActive) return;
                if (rec.layers <= 0) return;
                if (dmg <= 0f) return;

                // 큰 피해면 한 방에 여러 겹이 깨질 수 있도록 루프 처리
                int brokenThisHit = 0;
                float original = dmg;

                while (rec.layers > 0 && dmg > 0f)
                {
                    float absorb = Mathf.Min(dmg, rec.perLayerAbsorb);
                    dmg -= absorb;
                    rec.layers--;
                    brokenThisHit++;

                    Debug.Log($"[TripleThinShield] {caster.name} 보호막 1겹 파괴! (흡수 {absorb:F1}) 남은겹:{rec.layers}");
                }

                if (brokenThisHit > 0)
                {
                    // 겹 파괴 시 1초간 피해감소 20% (중첩 X, 리프레시)
                    TriggerBreakDamageReduction(caster, rec);

                    // 발동 즉시를 어느 정도 반영하기 위해:
                    // 이 히트에서 보호막 흡수 후 남은 피해에도 20% 감쇠를 1회 적용(중첩X)
                    // (TakeDamage의 damageReduction 계산은 훅보다 앞이라, 이 부분이 없으면 다음 히트부터만 적용됨)
                    if (dmg > 0f)
                        dmg *= (1f - rec.breakDR);

                    Debug.Log($"[TripleThinShield] {caster.name} 남은 피해 {original:F1} → {dmg:F1} (흡수/즉시감쇠 반영)");
                }

                // 겹이 전부 소진되면 보호막은 즉시 종료(8초 남았어도 끝)
                if (rec.layers <= 0)
                {
                    EndShield(caster, rec);
                }
            };

            caster.OnBeforeTakeDamage += rec.beforeHandler;
            rec.hooked = true;
        }

        Debug.Log($"[TripleThinShield] {caster.name} 3겹 보호막 생성! (겹당 {rec.perLayerAbsorb:F1}, 지속 {duration:F1}s)");
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null) return;
        if (!_map.TryGetValue(caster, out var rec)) return;

        // 루틴 정리
        if (rec.shieldExpireRoutine != null) caster.StopCoroutine(rec.shieldExpireRoutine);
        rec.shieldExpireRoutine = null;

        // DR 정리(혹시 남아있으면 제거)
        if (rec.drRoutine != null) caster.StopCoroutine(rec.drRoutine);
        rec.drRoutine = null;
        if (rec.drApplied)
        {
            caster.ModifyStat(BuffStat.DamageReduction, rec.breakDR, isPercent: false, isRemove: true);
            rec.drApplied = false;
        }

        // 훅 해제
        if (rec.hooked && rec.beforeHandler != null)
        {
            caster.OnBeforeTakeDamage -= rec.beforeHandler;
            rec.hooked = false;
            rec.beforeHandler = null;
        }

        rec.shieldActive = false;
        rec.layers = 0;

        _map.Remove(caster);
    }

    private static IEnumerator CoExpireShield(UnitCombatFSM caster, Rec rec, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (caster == null || !caster.IsAlive()) yield break;

        // 시간 만료 시 남은 겹 소멸
        EndShield(caster, rec);
        Debug.Log($"[TripleThinShield] {caster.name} 보호막 지속시간 만료 → 종료");
    }

    private static void EndShield(UnitCombatFSM caster, Rec rec)
    {
        if (!rec.shieldActive) return;

        rec.shieldActive = false;
        rec.layers = 0;

        // 만료 루틴 정리
        if (rec.shieldExpireRoutine != null)
        {
            caster.StopCoroutine(rec.shieldExpireRoutine);
            rec.shieldExpireRoutine = null;
        }

        // 보호막 훅 해제(보호막 끝났으니 더 이상 흡수 X)
        if (rec.hooked && rec.beforeHandler != null)
        {
            caster.OnBeforeTakeDamage -= rec.beforeHandler;
            rec.hooked = false;
            // beforeHandler는 재시전 때 재사용 가능하므로 null로 안 만들어도 되지만,
            // 깔끔하게 하려면 null 처리해도 OK
        }
    }

    private static void TriggerBreakDamageReduction(UnitCombatFSM caster, Rec rec)
    {
        // 이미 걸려있으면 “리프레시” (중첩 X)
        if (rec.drRoutine != null)
            caster.StopCoroutine(rec.drRoutine);

        if (!rec.drApplied)
        {
            caster.ModifyStat(BuffStat.DamageReduction, rec.breakDR, isPercent: false, isRemove: false);
            rec.drApplied = true;
        }

        rec.drRoutine = caster.StartCoroutine(CoRemoveBreakDR(caster, rec));
    }

    private static IEnumerator CoRemoveBreakDR(UnitCombatFSM caster, Rec rec)
    {
        yield return new WaitForSeconds(rec.breakDRDuration);
        if (caster == null || !caster.IsAlive()) yield break;

        if (rec.drApplied)
        {
            caster.ModifyStat(BuffStat.DamageReduction, rec.breakDR, isPercent: false, isRemove: true);
            rec.drApplied = false;
        }

        rec.drRoutine = null;
    }
}

// 독창 투척 스킬: ThrowSpearAttack 흐름 재활용(가까운 적에게 원거리 공격)
// - 피해: 공격력 * 1.5
// - 중독: 최대 2중첩, 6틱/초, 초당 MaxHP의 5% 피해 (아래 PoisonSystem 참고)
//
// SkillEffect 파라미터(권장 매핑)
// - effect.skillValue     : 직접 피해 배율 (기본 1.5)
// - effect.skillRange     : 스킬 사거리(필수 SkillExecutor가 이 값으로 이동/시전 판단)
// - effect.skillDuration  : 중독 지속시간(초)  (미지정이면 기본값 적용: 아래 참고)
// - effect.skillMaxStack  : 중독 최대 중첩 (기본 2)
// - effect.skillDelayTime : 중독 초당 최대체력 비율 (기본 0.05 = 5%/s)
public class ThrowSpearPoisonAttackSkill : ISkillBehavior
{
    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        var target = FindTarget(caster, effect);
        if (target == null) return false;

        // SkillExecutor가 effect.skillRange로 사거리/이동을 결정함 → 여기서도 동일 값 사용
        float range = (effect != null && effect.skillRange > 0f) ? effect.skillRange : 0f;
        if (range <= 0f)
        {
            Debug.LogWarning("[ThrowSpearPoisonAttack] skillRange가 0 이하라 스킬이 정상 시전되지 않을 수 있음. SkillEffect.skillRange를 설정하세요.");
            return false;
        }

        float dist = Vector3.Distance(caster.transform.position, target.transform.position);
        return dist <= range && caster.CanUseSkill();
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        return caster.FindNearestEnemy(); // ThrowSpearAttack와 동일
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null || target == null) return;

        // 1) 직접 피해(공격력 1.5배)
        float dmgMul = (effect != null && effect.skillValue > 0f) ? effect.skillValue : 1.5f;
        float damage = caster.stats.attack * dmgMul;

        // attacker 전달: 이후 피해 후처리/킬 크레딧 같은 확장
        target.TakeDamage(damage, caster);

        // 2) 중독 적용
        int maxStack = (effect != null && effect.skillMaxStack > 0f) ? Mathf.RoundToInt(effect.skillMaxStack) : 2;
      

        PoisonSystem.ApplyPoison(
            target: target,
            source: caster,
            percentPerTick: 0.05f, // 틱당 MaxHP 5%
            maxStack: 2,           // 최대 2중첩 고정
            totalTicks: 6          // 총 6틱 고정(리프레시 포함)
        );

        Debug.Log($"[독창 투척] {caster.name} → {target.name} 피해 {damage:F1} + 중독({maxStack}스택)");
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
}


// 단일 아군 HoT
// - 아군 1명(기본: 가장 체력 비율이 낮은 아군)에게
// - 5초 동안, 1초마다(총 5틱) 최대체력의 7%씩 회복 = 총 35%
// - 쿨타임은 SkillData.skillCoolDown(8초)로 제어됨
//
// SkillEffect 파라미터 매핑(권장)
// - effect.skillValue     : 초당 회복 비율(기본 0.07 = 7%)
// - effect.skillDuration  : 총 지속시간(기본 5초)
// - effect.skillDelayTime : 틱 간격(기본 1초)
// - effect.skillRange     : 힐 사거리(필수 권장. 0이면 SkillExecutor가 "즉시 시전" 처리해서 이동 없이 발동함)
public class HealOverTimeSkill : ISkillBehavior
{
    // caster별 HoT 1개만 유지(재시전 시 이전 HoT 종료 후 새로 시작)
    private static readonly Dictionary<UnitCombatFSM, Coroutine> _active = new();

    private const float DefaultHealPctPerTick = 0.07f; // 7%/sec
    private const float DefaultDuration = 5f;          // 5 sec
    private const float DefaultTickInterval = 1f;      // 1 sec

    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null || !caster.CanUseSkill()) return false;

        var t = FindTarget(caster, effect);
        return t != null && t.IsAlive() && t.currentHP < t.stats.health;
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        // 현재 구현된 FindLowestHpAlly()는 "자기 자신 제외" 로직이 있음.
        // 만약 자기 자신도 포함하고 싶으면, 유틸 함수를 따로 만들거나 여기서 별도 탐색으로 바꾸면 됨.
        return caster.FindLowestHpAlly();
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null || !caster.IsAlive()) return;
        if (target == null || !target.IsAlive()) return;

        // 기존 HoT가 돌고 있으면 중단(중복/과힐 방지)
        if (_active.TryGetValue(caster, out var running) && running != null)
        {
            caster.StopCoroutine(running);
            _active.Remove(caster);
        }

        float healPctPerTick = (effect != null && effect.skillValue > 0f) ? effect.skillValue : DefaultHealPctPerTick;
        float duration = (effect != null && effect.skillDuration > 0f) ? effect.skillDuration : DefaultDuration;
        float tick = (effect != null && effect.skillDelayTime > 0f) ? effect.skillDelayTime : DefaultTickInterval;

        // "최대 체력의 35%" 문구를 일관되게 하려면 스냅샷이 안정적
        // (HoT 도중 MaxHP가 바뀌어도 회복량이 흔들리지 않음)
        float maxHpSnapshot = target.stats.health;

        var co = caster.StartCoroutine(CoHealOverTime(caster, target, maxHpSnapshot, healPctPerTick, duration, tick));
        _active[caster] = co;

        Debug.Log($"[HealOverTime] {caster.name} → {target.name} HoT 시작 (dur={duration:F1}s, tick={tick:F1}s, heal/tick={healPctPerTick:P0} of MaxHP)");
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null) return;

        if (_active.TryGetValue(caster, out var running) && running != null)
        {
            caster.StopCoroutine(running);
            _active.Remove(caster);
        }
    }

    private static IEnumerator CoHealOverTime(
        UnitCombatFSM caster,
        UnitCombatFSM target,
        float maxHpSnapshot,
        float healPctPerTick,
        float duration,
        float tickInterval
    )
    {
        float endTime = Time.time + duration;

        while (caster != null && caster.IsAlive() &&
               target != null && target.IsAlive() &&
               Time.time < endTime)
        {
            float healAmount = maxHpSnapshot * healPctPerTick; // 7% * MaxHP
            if (healAmount > 0f)
            {
                target.ReceiveHealing(healAmount);
            }

            yield return new WaitForSeconds(tickInterval);
        }

        if (caster != null)
            _active.Remove(caster);
    }
}

// 이동 불가 + 주변 아군 오라 패시브
// - 범위 내 아군: 공격력 +15%(퍼센트), 방어력 +15%(퍼센트),
//               받피감 +10%(DamageReduction 가산),
//               공속 +10%(주의: 이 프로젝트 attackSpeed는 공격간격(초)이므로 -10%로 적용)
// - 본인: 이동 불가( MoveState 진입 차단 + NavMeshAgent 정지 )
// - 항상 발동(패시브) / 쿨타임 없음
// - 업그레이드형은 buffstat으로 처리 코드안 switch문 확인
public class ImmobileAuraBuffSkill : ISkillBehavior
{
    private class BuffPack
    {
        public bool applied;
    }

    private class Rec
    {
        public Coroutine loop;
        public float radius;

        // 캐스터 이동 봉인 복구용
        public float prevAgentSpeed;
        public bool prevAgentStopped;
        public bool prevMovementLocked;

        // 대상별 버프 적용 여부
        public Dictionary<UnitCombatFSM, BuffPack> buffed = new();

        // 링 표시
        public LineRenderer auraRangeIndicator;

        // 설정 잠금: 첫 프레임 뒤(모든 effect 반영 후) lock
        public bool locked = false;

        // 버프 스펙(기본값)
        public float atkPct = 0.15f;
        public float defPct = 0.15f;
        public float dmgReductionAdd = 0.10f;

        // 이 프로젝트는 attackSpeed가 "공격간격(초)"로 쓰임
        // 공속 +10% => 공격간격 10% 감소 => -0.10 저장
        public float attackSpeedPct = -0.10f;

        // 보호막 펄스(강화 옵션)
        public bool enableBarrierPulse = false;
        public float barrierInterval = 10f;  // 10초
        public float barrierRatio = 0.20f;   // MaxHP의 20%
        public float barrierDuration = 5f;   // 5초 유지
        public float barrierTimer = 0f;
    }

    private static readonly Dictionary<UnitCombatFSM, Rec> _map = new();

    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect) => true;
    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect) => null;

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null) return;

        // 컨트롤 레코드 확보(없으면 생성)
        if (!_map.TryGetValue(caster, out var rec))
        {
            rec = new Rec();

            // 범위: effect.skillRange 우선, 없으면 기본값
            rec.radius = (effect != null && effect.skillRange > 0f)
                ? effect.skillRange
                : caster.stats.attackDistance * 2f;

            // 이동 봉인 적용
            rec.prevMovementLocked = caster.movementLocked;
            caster.movementLocked = true;

            if (caster.agent != null)
            {
                rec.prevAgentSpeed = caster.agent.speed;
                rec.prevAgentStopped = caster.agent.isStopped;

                caster.agent.ResetPath();
                caster.agent.isStopped = true;
                caster.agent.speed = 0f;
            }

            // 링 표시(최초 1회)
            rec.auraRangeIndicator = CreateAuraRangeIndicator(caster, rec.radius);

            // 오라 루프 시작(첫 틱은 1프레임 뒤)
            rec.loop = caster.StartCoroutine(AuraLoop(caster, rec));
            _map[caster] = rec;

            Debug.Log($"[ImmobileAuraBuff] {caster.name} 오라 시작 (radius={rec.radius:F1}) + 이동불가");
        }

        // effect가 여러 개면 Execute가 여러 번 호출되므로 설정 누적
        ApplyEffectToRec(rec, caster, effect);

        // 링 갱신
        if (rec.auraRangeIndicator != null)
            UpdateCircleWorld(rec.auraRangeIndicator, caster.transform.position, rec.radius);
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null) return;
        if (!_map.TryGetValue(caster, out var rec)) return;

        // 코루틴 종료
        if (rec.loop != null)
            caster.StopCoroutine(rec.loop);

        // 남아있는 버프 전부 해제
        foreach (var kv in rec.buffed.ToList())
        {
            var ally = kv.Key;
            if (ally == null) continue;
            RemoveBuffsFromAlly(ally, rec);
        }
        rec.buffed.Clear();

        // 이동 봉인 복구
        caster.movementLocked = rec.prevMovementLocked;

        if (caster.agent != null)
        {
            if (caster.movementLocked)
            {
                caster.agent.ResetPath();
                caster.agent.isStopped = true;
                caster.agent.speed = 0f;
            }
            else
            {
                caster.agent.isStopped = rec.prevAgentStopped;
                caster.agent.speed = rec.prevAgentSpeed;
            }
        }

        _map.Remove(caster);
        Debug.Log($"[ImmobileAuraBuff] {caster.name} 오라 종료/정리");

        // 링 제거
        if (rec.auraRangeIndicator != null)
        {
            UnityEngine.Object.Destroy(rec.auraRangeIndicator.gameObject);
            rec.auraRangeIndicator = null;
        }
    }

    private static void ApplyEffectToRec(Rec rec, UnitCombatFSM caster, SkillEffect effect)
    {
        if (rec == null || caster == null || effect == null) return;

        // 한번 lock 된 뒤에는 값 변경 금지(적용/해제 불일치 방지)
        if (rec.locked) return;

        // radius는 가장 큰 값 유지
        if (effect.skillRange > 0f)
            rec.radius = Mathf.Max(rec.radius, effect.skillRange);

        switch (effect.buffStat)
        {
            case BuffStat.Attack:
                if (effect.skillValue != 0f) rec.atkPct = effect.skillValue; // 예: 0.20
                break;

            case BuffStat.Defense:
                if (effect.skillValue != 0f) rec.defPct = effect.skillValue; // 예: 0.25
                break;

            case BuffStat.DamageReduction:
                if (effect.skillValue != 0f) rec.dmgReductionAdd = effect.skillValue; // 예: 0.15
                break;

            case BuffStat.AttackSpeed:
                // 인스펙터에는 +0.15로 넣고, 실제는 공격간격 감소이므로 음수로 저장
                if (effect.skillValue != 0f)
                    rec.attackSpeedPct = -Mathf.Abs(effect.skillValue); // 예: -0.15
                break;

            case BuffStat.None:
                // 보호막 펄스 설정:
                // - interval: skillDelayTime
                // - ratio   : skillValue
                // - duration: skillMaxStack (skillDuration은 0이어야 패시브 적용됨)
                if (effect.skillDelayTime > 0f && effect.skillValue > 0f && effect.skillMaxStack > 0f)
                {
                    rec.enableBarrierPulse = true;
                    rec.barrierInterval = effect.skillDelayTime; // 10
                    rec.barrierRatio = effect.skillValue;        // 0.20
                    rec.barrierDuration = effect.skillMaxStack;  // 5
                }
                break;
        }
    }

    private static IEnumerator AuraLoop(UnitCombatFSM caster, Rec rec)
    {
        // 중요: 모든 effect Execute가 끝난 다음 프레임부터 적용 시작
        yield return null;

        // 이 시점의 설정값을 고정(적용/해제 불일치 방지)
        rec.locked = true;

        const float interval = 0.25f;

        while (caster != null)
        {
            if (!caster.IsAlive())
            {
                // 캐스터 사망 시 정리
                new ImmobileAuraBuffSkill().Remove(caster, null);
                yield break;
            }

            // 이동 봉인 유지
            if (caster.agent != null)
            {
                caster.agent.isStopped = true;
                caster.agent.speed = 0f;
            }
            caster.movementLocked = true;

            // 링 중심/반경 갱신
            if (rec.auraRangeIndicator != null)
                UpdateCircleWorld(rec.auraRangeIndicator, caster.transform.position, rec.radius);

            // 범위 내 아군 찾기
            var allies = GameObject.FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None)
                .Where(u => u != null && u.IsAlive()
                            && u.unitData.faction == caster.unitData.faction
                            && u != caster);

            HashSet<UnitCombatFSM> valid = new();

            foreach (var ally in allies)
            {
                float dist = Vector3.Distance(caster.transform.position, ally.transform.position);
                if (dist <= rec.radius)
                {
                    valid.Add(ally);

                    if (!rec.buffed.ContainsKey(ally))
                    {
                        ApplyBuffsToAlly(ally, rec);
                        rec.buffed[ally] = new BuffPack { applied = true };
                    }
                }
            }

            // 범위 밖으로 나간 대상 버프 해제
            var toRemove = rec.buffed.Keys.Where(u => u == null || !valid.Contains(u)).ToList();
            foreach (var u in toRemove)
            {
                if (u != null)
                    RemoveBuffsFromAlly(u, rec);

                rec.buffed.Remove(u);
            }

            // 보호막 펄스(강화 옵션)
            if (rec.enableBarrierPulse)
            {
                rec.barrierTimer += interval;
                if (rec.barrierTimer >= rec.barrierInterval)
                {
                    rec.barrierTimer = 0f;

                    float amount = caster.stats.health * rec.barrierRatio;
                    foreach (var ally in valid)
                    {
                        if (ally == null || !ally.IsAlive()) continue;
                        ally.ApplyBarrier(amount, rec.barrierDuration);
                    }
                }
            }

            yield return new WaitForSeconds(interval);
        }
    }

    private static void ApplyBuffsToAlly(UnitCombatFSM ally, Rec rec)
    {
        ally.ModifyStat(BuffStat.Attack, rec.atkPct, true, false);
        ally.ModifyStat(BuffStat.Defense, rec.defPct, true, false);
        ally.ModifyStat(BuffStat.DamageReduction, rec.dmgReductionAdd, false, false);
        ally.ModifyStat(BuffStat.AttackSpeed, rec.attackSpeedPct, true, false);
    }

    private static void RemoveBuffsFromAlly(UnitCombatFSM ally, Rec rec)
    {
        ally.ModifyStat(BuffStat.Attack, rec.atkPct, true, true);
        ally.ModifyStat(BuffStat.Defense, rec.defPct, true, true);
        ally.ModifyStat(BuffStat.DamageReduction, rec.dmgReductionAdd, false, true);
        ally.ModifyStat(BuffStat.AttackSpeed, rec.attackSpeedPct, true, true);
    }

    private const int CircleSegments = 50;
    private const float RingHeight = 0.05f;
    private const float RingWidth = 0.05f;
    private static Material s_RingMat;

    private static LineRenderer CreateAuraRangeIndicator(UnitCombatFSM caster, float radius)
    {
        GameObject obj = new GameObject("AuraRangeIndicator");

        obj.transform.position = caster.transform.position;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        var lr = obj.AddComponent<LineRenderer>();
        lr.positionCount = CircleSegments + 1;
        lr.loop = true;
        lr.widthMultiplier = RingWidth;
        lr.useWorldSpace = true;

        if (s_RingMat == null)
            s_RingMat = new Material(Shader.Find("Sprites/Default"));

        lr.material = s_RingMat;
        lr.startColor = Color.green;
        lr.endColor = Color.green;

        UpdateCircleWorld(lr, caster.transform.position, radius);
        return lr;
    }

    private static void UpdateCircleWorld(LineRenderer lr, Vector3 center, float radius)
    {
        if (lr == null) return;

        for (int i = 0; i <= CircleSegments; i++)
        {
            float t = (float)i / CircleSegments;
            float angle = t * Mathf.PI * 2f;

            Vector3 pos = center + new Vector3(
                Mathf.Cos(angle) * radius,
                RingHeight,
                Mathf.Sin(angle) * radius
            );

            lr.SetPosition(i, pos);
        }
    }
}


// public class ImmobileAuraBuffSkill : ISkillBehavior
// {
//     private class BuffPack
//     {
//         public bool applied;
//     }

//     private class Rec
//     {
//         public Coroutine loop;
//         public float radius;

//         // 캐스터 이동 봉인 복구용
//         public float prevAgentSpeed;
//         public bool  prevAgentStopped;
//         public bool  prevMovementLocked;

//         // 대상별 버프 적용 여부(소스별로 분리되므로 오라 중첩도 가능)
//         public Dictionary<UnitCombatFSM, BuffPack> buffed = new();

//         public LineRenderer auraRangeIndicator;
//     }

//     private static readonly Dictionary<UnitCombatFSM, Rec> _map = new();

//     // 고정 스펙(요구사항)
//     private const float atkPct = 0.15f;
//     private const float defPct = 0.15f;
//     private const float dmgReductionAdd = 0.10f;

//     // 이 프로젝트는 attackSpeed가 "공격 속도"가 아니라 "공격 간격(초)"로 쓰임
//     // 그래서 공속 +10% = 공격 간격 10% 감소 = -0.10으로 적용해야 더 빨라짐
//     private const float attackSpeedPct = -0.10f;

//     public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect) => true;
//     public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect) => null;

//     public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
//     {
//         if (caster == null) return;
//         if (_map.ContainsKey(caster)) return; // 중복 실행 방지

//         var rec = new Rec();

//         // 오라 범위: effect.skillRange를 우선 사용
//         rec.radius = (effect != null && effect.skillRange > 0f)
//             ? effect.skillRange
//             : caster.stats.attackDistance * 2f; // 기본값(없으면 대충)

//         //이동 봉인 적용
//         rec.prevMovementLocked = caster.movementLocked;
//         caster.movementLocked = true;

//         if (caster.agent != null)
//         {
//             rec.prevAgentSpeed = caster.agent.speed;
//             rec.prevAgentStopped = caster.agent.isStopped;

//             caster.agent.ResetPath();
//             caster.agent.isStopped = true;
//             caster.agent.speed = 0f;
//         }

//         rec.loop = caster.StartCoroutine(AuraLoop(caster, rec));

//         _map[caster] = rec;
//         Debug.Log($"[ImmobileAuraBuff] {caster.name} 오라 시작 (radius={rec.radius:F1}) + 이동불가");

//         //오라 범위 링 표시
//         rec.auraRangeIndicator = CreateAuraRangeIndicator(caster, rec.radius);
//     }

//     public void Remove(UnitCombatFSM caster, SkillEffect effect)
//     {
//         if (caster == null) return;
//         if (!_map.TryGetValue(caster, out var rec)) return;

//         // 코루틴 종료
//         if (rec.loop != null)
//             caster.StopCoroutine(rec.loop);

//         // 남아있는 버프 전부 해제
//         foreach (var kv in rec.buffed.ToList())
//         {
//             var ally = kv.Key;
//             if (ally == null) continue;
//             RemoveBuffsFromAlly(ally);
//         }
//         rec.buffed.Clear();

//         // 이동 봉인 복구(사망 시엔 의미 없지만 안전하게)
//         caster.movementLocked = rec.prevMovementLocked;
//         if (caster.agent != null)
//         {
//             caster.agent.isStopped = rec.prevAgentStopped;
//             caster.agent.speed = caster.movementLocked ? 0f : caster.stats.moveSpeed;
//         }

//         _map.Remove(caster);
//         Debug.Log($"[ImmobileAuraBuff] {caster.name} 오라 종료/정리");
//         //오라 범위 링 제거
//         if (rec.auraRangeIndicator != null)
//         {
//             UnityEngine.Object.Destroy(rec.auraRangeIndicator.gameObject);
//             rec.auraRangeIndicator = null;
//         }
//     }

//     private static IEnumerator AuraLoop(UnitCombatFSM caster, Rec rec)
//     {
//         // 너무 잦으면 부담, 너무 길면 반응 느림 → 0.25~0.5 권장
//         const float interval = 0.25f;

//         while (caster != null)
//         {
//             if (!caster.IsAlive())
//             {
//                 // 캐스터 사망 시 정리
//                 new ImmobileAuraBuffSkill().Remove(caster, null);
//                 yield break;
//             }

//             // 혹시 다른 코드가 풀어버렸으면 계속 고정
//             if (caster.agent != null)
//             {
//                 caster.agent.isStopped = true;
//                 caster.agent.speed = 0f;
//             }
//             caster.movementLocked = true;

//             var allies = GameObject.FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None)
//                 .Where(u => u != null && u.IsAlive()
//                             && u.unitData.faction == caster.unitData.faction
//                             && u != caster); // 기본은 자기 자신 제외(원하면 포함 가능)

//             HashSet<UnitCombatFSM> valid = new();

//             foreach (var ally in allies)
//             {
//                 float dist = Vector3.Distance(caster.transform.position, ally.transform.position);
//                 if (dist <= rec.radius)
//                 {
//                     valid.Add(ally);

//                     if (!rec.buffed.ContainsKey(ally))
//                     {
//                         ApplyBuffsToAlly(ally);
//                         rec.buffed[ally] = new BuffPack { applied = true };
//                         // Debug.Log($"[ImmobileAuraBuff] {caster.name} → {ally.name} 버프 적용");
//                     }
//                 }
//             }

//             // 범위 밖/사망 등으로 빠진 대상 버프 해제
//             var toRemove = rec.buffed.Keys.Where(u => u == null || !valid.Contains(u)).ToList();
//             foreach (var u in toRemove)
//             {
//                 if (u != null)
//                 {
//                     RemoveBuffsFromAlly(u);
//                     // Debug.Log($"[ImmobileAuraBuff] {caster.name} → {u.name} 버프 해제");
//                 }
//                 rec.buffed.Remove(u);
//             }

//             yield return new WaitForSeconds(interval);
//         }
//     }

//     private static void ApplyBuffsToAlly(UnitCombatFSM ally)
//     {
//         // 공격/방어는 퍼센트(곱)
//         ally.ModifyStat(BuffStat.Attack,  atkPct, true, false);
//         ally.ModifyStat(BuffStat.Defense, defPct, true, false);

//         // 받는 피해 감소는 DamageReduction 가산(+0.10 => 10% 감소)
//         ally.ModifyStat(BuffStat.DamageReduction, dmgReductionAdd, false, false);

//         // 공속(공격간격) 10% 감소 => 더 빨라짐
//         ally.ModifyStat(BuffStat.AttackSpeed, attackSpeedPct, true, false);
//     }

//     private static void RemoveBuffsFromAlly(UnitCombatFSM ally)
//     {
//         ally.ModifyStat(BuffStat.Attack,  atkPct, true, true);
//         ally.ModifyStat(BuffStat.Defense, defPct, true, true);
//         ally.ModifyStat(BuffStat.DamageReduction, dmgReductionAdd, false, true);
//         ally.ModifyStat(BuffStat.AttackSpeed, attackSpeedPct, true, true);
//     }

//     private const int CircleSegments = 50;
//     private const float RingHeight = 0.05f;
//     private const float RingWidth = 0.05f;
//     private static Material s_RingMat;

//     private static LineRenderer CreateAuraRangeIndicator(UnitCombatFSM caster, float radius)
//     {
//         GameObject obj = new GameObject("AuraRangeIndicator");

//         //스케일 영향 안 받게 부모에 안 붙임
//         obj.transform.position = caster.transform.position;
//         obj.transform.rotation = Quaternion.identity;
//         obj.transform.localScale = Vector3.one;

//         var lr = obj.AddComponent<LineRenderer>();
//         lr.positionCount = CircleSegments + 1;
//         lr.loop = true;
//         lr.widthMultiplier = RingWidth;

//         //월드 좌표로 그리기
//         lr.useWorldSpace = true;

//         if (s_RingMat == null)
//             s_RingMat = new Material(Shader.Find("Sprites/Default"));

//         lr.material = s_RingMat;
//         lr.startColor = Color.green;
//         lr.endColor = Color.green;

//         UpdateCircleWorld(lr, caster.transform.position, radius);
//         return lr;
//     }

//     private static void UpdateCircleWorld(LineRenderer lr, Vector3 center, float radius)
//     {
//         if (lr == null) return;

//         for (int i = 0; i <= CircleSegments; i++)
//         {
//             float t = (float)i / CircleSegments;
//             float angle = t * Mathf.PI * 2f;

//             Vector3 pos = center + new Vector3(
//                 Mathf.Cos(angle) * radius,
//                 RingHeight,
//                 Mathf.Sin(angle) * radius
//             );

//             lr.SetPosition(i, pos);
//         }
//     }
// }



/// <summary>
/// 근처 아군 3명에게 재생상태효과를 부여하는 스킬
///
/// SkillEffect 매핑(권장)
/// - skillType      : UnitSkillType.Regen
/// - skillRange     : 판정 반경(필수 권장)
/// - skillValue     : 회복량(퍼센트면 0.10 = 10%)
/// - isPercent      : true면 MaxHP * skillValue, false면 절대값
/// - skillDuration  : 재생 지속시간(초) (요구사항: 4초로 세팅)
/// - skillDelayTime : 틱 간격(초) (요구사항이 "초당"이면 1초로 세팅)
/// </summary>
public class RegenNearbyAlliesSkill : ISkillBehavior
{
    private struct Candidate
    {
        public UnitCombatFSM unit;
        public float distSqr;
    }

    private const int TargetCount = 3;

    // 요구사항 기본값(인스펙터에서 미세조정 가능하게 fallback만 둠)
    private const float DefaultDuration = 4f;
    private const float DefaultTickInterval = 1f;

    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null || !caster.CanUseSkill()) return false;

        // 스펙상 "근처" 기반이므로 반경이 없으면 발동 자체를 막는다(쿨다운 낭비 방지).
        if (effect == null || effect.skillRange <= 0f) return false;

        // 회복량이 0 이하면 의미가 없으니 발동 막기
        if (effect.skillValue <= 0f) return false;

        // 아군이 아예 없으면 발동할 이유가 없음
        return caster.FindNearestAlly() != null;
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        // SkillExecutor가 사거리 밖이면 이동을 처리하므로,
        // 이동 기준점이 될 대표 타겟(가장 가까운 아군) 하나를 반환.
        return caster.FindNearestAlly();
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null || !caster.IsAlive()) return;
        if (effect == null) return;

        float radius = effect.skillRange;
        if (radius <= 0f) return;

        float duration = (effect.skillDuration > 0f) ? effect.skillDuration : DefaultDuration;

        // SkillEffect의 필드명이 DelayTime이지만, 이 프로젝트에서 HoT 틱 간격으로도 재사용 중이라 동일 패턴 유지
        float tickInterval = (effect.skillDelayTime > 0f) ? effect.skillDelayTime : DefaultTickInterval;

        float amount = effect.skillValue;
        bool isPercent = effect.isPercent;

        var all = GameObject.FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None);

        Vector3 cpos = caster.transform.position;
        float r2 = radius * radius;

        // 후보 수집
        List<Candidate> candidates = new List<Candidate>(8);
        for (int i = 0; i < all.Length; i++)
        {
            UnitCombatFSM u = all[i];
            if (u == null) continue;
            if (u == caster) continue;
            if (!u.IsAlive()) continue;
            if (u.unitData.faction != caster.unitData.faction) continue;

            Vector3 p = u.transform.position;

            // XZ 기준 거리(높이 무시)로 근처 판정
            float dx = p.x - cpos.x;
            float dz = p.z - cpos.z;
            float d2 = dx * dx + dz * dz;

            if (d2 > r2) continue;

            candidates.Add(new Candidate { unit = u, distSqr = d2 });
        }

        if (candidates.Count == 0) return;

        // 거리 오름차순 정렬 후 3명 적용
        candidates.Sort((a, b) => a.distSqr.CompareTo(b.distSqr));

        int applied = 0;
        for (int i = 0; i < candidates.Count && applied < TargetCount; i++)
        {
            UnitCombatFSM ally = candidates[i].unit;
            if (ally == null || !ally.IsAlive()) continue;

            // "재생 상태효과" 재활용: RegenStatus가 없으면 붙이고, 있으면 갱신(StartPulse 내부에서 Stop 후 재시작)
            RegenStatus regen = ally.GetComponent<RegenStatus>();
            if (regen == null) regen = ally.gameObject.AddComponent<RegenStatus>();

            regen.StartPulse(tickInterval, amount, isPercent, duration);
            applied++;
        }

        Debug.Log($"[RegenNearbyAllies] {caster.name} → {applied} allies (r={radius}, dur={duration}, tick={tickInterval}, val={amount}, percent={isPercent})");
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect)
    {
        // 이 스킬은 부여형이고, RegenStatus가 duration 종료로 알아서 끝남.
        // 패시브 해제형이 필요해지면 (예: 오라가 꺼질 때 즉시 StopPulse) 여기에서 처리.
    }
}

public class TargetedAoeBlindSkill : ISkillBehavior
{
    // 스킬 사양
    // - effect.skillRange     : 시전 사거리(캐스터 -> 타겟까지 거리)
    // - effect.skillMaxStack  : AoE 반경(원형 범위기 반경)
    // - effect.skillDuration  : 실명 지속시간(초) = 3
    // - effect.skillDelayTime : 투사체/발동 지연(선택, 0이면 즉시)

    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        //if (caster == null || effect == null) return false;
        if (!caster.CanUseSkill()) return false;

        // 기존 타겟이 유효하면 그대로
        // if (caster.targetEnemy != null && caster.targetEnemy.IsAlive() &&
        //     caster.targetEnemy.unitData.faction != caster.unitData.faction)
        // {
        //     return true;
        // }

        // TargetingUtil로 가장 가까운 적을 가져와 캐시에 저장
        caster.targetEnemy = TargetingUtil.FindNearestEnemyGlobal(caster);
        return caster.targetEnemy != null;
        
    }
    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null) return null;

        // ShouldTrigger에서 이미 targetEnemy를 확보해두는 구조
        if (caster.targetEnemy != null && caster.targetEnemy.IsAlive() &&
            caster.targetEnemy.unitData.faction != caster.unitData.faction)
        {
            return caster.targetEnemy;
        }

        // 안전하게 한 번 더(타겟이 바로 죽었을 수 있음)
        caster.targetEnemy = TargetingUtil.FindNearestEnemyGlobal(caster);
        return caster.targetEnemy;
    }
    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null || effect == null) return;
        if (!caster.IsAlive()) return;
        if (target == null || !target.IsAlive()) return;

        float delay = effect.skillDelayTime;

        if (delay > 0f)
        {
            caster.StartCoroutine(CoApplyAfterDelay(caster, target, effect, delay));
            return;
        }

        // 즉시 발동은 SkillExecutor가 사거리 보장한다는 전제 하에 바로 적용
        ApplyBlindAoe(caster, target, effect);
    }

    private IEnumerator CoApplyAfterDelay(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (caster == null || target == null) yield break;
        if (!caster.IsAlive() || !target.IsAlive()) yield break;

        // 딜레이가 있는 경우에만 사거리 재검증(권장)
        float castRange = effect.skillRange;
        float dist = Vector3.Distance(caster.transform.position, target.transform.position);
        if (dist > castRange) yield break;

        ApplyBlindAoe(caster, target, effect);
    }

    private void ApplyBlindAoe(UnitCombatFSM caster, UnitCombatFSM centerTarget, SkillEffect effect)
    {
        float radius = effect.skillMaxStack; // AoE 반경
        float duration = effect.skillDuration; // 실명 지속

        if (radius <= 0f || duration <= 0f) return;

        Vector3 center = centerTarget.transform.position;
        float r2 = radius * radius;

        var all = GameObject.FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None);

        int applied = 0;

        for (int i = 0; i < all.Length; i++)
        {
            UnitCombatFSM enemy = all[i];
            if (enemy == null || !enemy.IsAlive()) continue;

            // 적만
            if (enemy.unitData.faction == caster.unitData.faction) continue;

            Vector3 p = enemy.transform.position;

            // XZ 거리 기반 원형 판정(높이 무시)
            float dx = p.x - center.x;
            float dz = p.z - center.z;
            float d2 = dx * dx + dz * dz;
            if (d2 > r2) continue;

            // 실명 상태효과 재활용
            if (enemy.blind == null)
            {
                // 보통 UnitCombatFSM.Start()에서 자동 부착되지만, 혹시 누락된 객체 대비
                var comp = enemy.GetComponent<BlindSystem>();
                if (comp == null) comp = enemy.gameObject.AddComponent<BlindSystem>();
                enemy.blind = comp;
            }

            enemy.blind.Apply(duration);
            applied++;
        }

        //Debug.Log($"[TargetedAoeBlind] {caster.name} -> center:{centerTarget.name}, applied:{applied}, r:{radius:F1}, dur:{duration:F1}");
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect){}
}


public class RegenNearbyAlliesUpgradeSkill : ISkillBehavior
{
    private const int DefaultMaxAllies = 3;

    private const float DefaultRegenDuration = 4f;     // 4초
    private const float DefaultRegenTick = 1f;         // 초당
    private const float DefaultRegenPctPerTick = 0.15f; // 최대체력의 15%/sec

    private const float MoveSpeedPct = 0.20f;          // 이속 20%
    private const float MoveSpeedDuration = 5f;        // 5초

    private struct Candidate
    {
        public UnitCombatFSM unit;
        public float d2;
    }

    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null || effect == null) return false;
        if (!caster.CanUseSkill()) return false;

        float radius = effect.skillRange;
        if (radius <= 0f) return false;

        int maxAllies = (effect.skillMaxStack > 0f) ? Mathf.RoundToInt(effect.skillMaxStack) : DefaultMaxAllies;
        if (maxAllies <= 0) maxAllies = DefaultMaxAllies;

        var allies = CollectNearestAllies(caster, radius, maxAllies);
        if (allies.Count == 0) return false;

        // 낭비 방지: "회복 필요" 또는 "눈에 보이는 해로운 상태"가 있으면 발동
        for (int i = 0; i < allies.Count; i++)
        {
            var a = allies[i];
            if (a == null || !a.IsAlive()) continue;

            if (a.currentHP < a.stats.health) return true;
            if (a.IsStunned()) return true;
            if (a.isSilenced) return true;
            if (a.blind != null && a.blind.IsBlinded) return true;

            // 출혈/중독은 현재 코드에서 "걸려있는지"를 빠르게 조회하는 public API가 없어서
            // 여기서는 트리거 조건에 포함하지 않음(Execute에서는 정화 시도함).
        }

        return false;
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        // 자기 주변 스킬이므로 caster 반환 (SkillExecutor의 이동 유도 방지)
        return caster;
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null || effect == null) return;

        float radius = effect.skillRange;
        if (radius <= 0f) return;

        int maxAllies = (effect.skillMaxStack > 0f) ? Mathf.RoundToInt(effect.skillMaxStack) : DefaultMaxAllies;
        if (maxAllies <= 0) maxAllies = DefaultMaxAllies;

        float regenDuration = (effect.skillDuration > 0f) ? effect.skillDuration : DefaultRegenDuration;
        float tickInterval  = (effect.skillDelayTime > 0f) ? effect.skillDelayTime : DefaultRegenTick;
        float healPctTick   = (effect.skillValue > 0f) ? effect.skillValue : DefaultRegenPctPerTick;

        var allies = CollectNearestAllies(caster, radius, maxAllies);

        for (int i = 0; i < allies.Count; i++)
        {
            var ally = allies[i];
            if (ally == null || !ally.IsAlive()) continue;

            // 1) 정화: 기절/침묵/실명/출혈/중독 + (가능한 경우) 추적형 스탯감소 디버프 제거
            StatusCleanseUtil.CleanseHarmful(
                ally,
                clearStun: true,
                clearSilence: true,
                clearBlind: true,
                clearBleed: true,
                clearPoison: true,
                clearStatDebuffs: true
            );

            // 2) 재생 부여(4초 동안 초당 최대체력의 15%)
            var regen = ally.GetComponent<RegenStatus>();
            if (regen == null) regen = ally.gameObject.AddComponent<RegenStatus>();
            regen.StartPulse(tickInterval, healPctTick, isPercent: true, duration: regenDuration);

            // 3) 이속 버프(기절 해제 이후에 적용해야 StunSystem 복구 로직에 안 덮임)
            ally.ApplyBuff(BuffStat.MoveSpeed, MoveSpeedPct, MoveSpeedDuration, isPercent: true);
        }

        // SkillExecutor에서도 reset하지만, 다른 경로 호출 가능성까지 고려해 안전하게 유지
        caster.skillTimer = 0f;
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }

    private static List<UnitCombatFSM> CollectNearestAllies(UnitCombatFSM caster, float radius, int maxCount)
    {
        var result = new List<UnitCombatFSM>(maxCount);

        if (caster == null || radius <= 0f || maxCount <= 0)
            return result;

        var all = GameObject.FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None);
        if (all == null || all.Length == 0)
            return result;

        float r2 = radius * radius;
        Vector3 cpos = caster.transform.position;

        var temp = new List<Candidate>(16);

        for (int i = 0; i < all.Length; i++)
        {
            var u = all[i];
            if (u == null) continue;
            if (u == caster) continue;
            if (!u.IsAlive()) continue;
            if (u.unitData == null || caster.unitData == null) continue;
            if (u.unitData.faction != caster.unitData.faction) continue;

            Vector3 upos = u.transform.position;

            float dx = upos.x - cpos.x;
            float dz = upos.z - cpos.z;
            float d2 = dx * dx + dz * dz;

            if (d2 > r2) continue;

            temp.Add(new Candidate { unit = u, d2 = d2 });
        }

        if (temp.Count == 0)
            return result;

        temp.Sort((a, b) => a.d2.CompareTo(b.d2));

        int take = Mathf.Min(maxCount, temp.Count);
        for (int i = 0; i < take; i++)
            result.Add(temp[i].unit);

        return result;
    }
}

public class NearestEnemyAoeStunThenBlindSkill : ISkillBehavior
{
    // 기본값(에셋 세팅이 비어 있어도 동작하도록)
    private const float DefaultStunSec = 2f;
    private const float DefaultBlindSec = 4f;
    private const float DefaultAoeRadius = 4f;

    // 동일 타겟에 대해 2초 후 실명 코루틴이 중복으로 쌓이는 것 방지
    private static readonly Dictionary<UnitCombatFSM, Coroutine> _pendingBlind = new();

    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null || effect == null) return false;
        if (!caster.CanUseSkill()) return false;

        // 시전 사거리 미세팅이면 발동 자체를 막아 쿨만 도는/이동만 하는 상황 방지
        if (effect.skillRange <= 0f) return false;

        return FindTarget(caster, effect) != null;
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null) return null;

        // 가장 가까운 적
        return TargetingUtil.FindNearestEnemyGlobal(caster, aliveOnly: true, xzOnly: true);
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null || effect == null) return;
        if (target == null || !target.IsAlive()) return;

        float aoeRadius = (effect.skillMaxStack > 0f) ? effect.skillMaxStack : DefaultAoeRadius;
        float stunSec = (effect.skillValue > 0f) ? effect.skillValue : DefaultStunSec;
        float blindSec = (effect.skillDuration > 0f) ? effect.skillDuration : DefaultBlindSec;

        Vector3 center = target.transform.position;

        // AoE 범위 내 적 수집
        var victims = CollectEnemiesInCircle(caster, center, aoeRadius);

        if (victims.Count == 0)
            return;

        // 1) 즉시 기절 적용
        for (int i = 0; i < victims.Count; i++)
        {
            var v = victims[i];
            if (v == null || !v.IsAlive()) continue;

            StunSystem.Apply(v, stunSec);

            // 2) 2초 후(=기절 이후) 실명 부여
            StartOrRestartDelayedBlind(v, stunSec, blindSec);
        }
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }

    private static List<UnitCombatFSM> CollectEnemiesInCircle(UnitCombatFSM caster, Vector3 center, float radius)
    {
        var result = new List<UnitCombatFSM>(16);
        if (caster == null || radius <= 0f) return result;

        float r2 = radius * radius;

        // 프로젝트 내 다른 AoE 스킬(FarthestDoubleAoeSkill 등)과 동일하게 OverlapSphere 사용
        var cols = Physics.OverlapSphere(center, radius, ~0);
        if (cols == null || cols.Length == 0) return result;

        var hit = new HashSet<UnitCombatFSM>();

        for (int i = 0; i < cols.Length; i++)
        {
            var col = cols[i];
            if (col == null) continue;

            var u = col.GetComponentInParent<UnitCombatFSM>();
            if (u == null) continue;
            if (!u.IsAlive()) continue;
            if (u.unitData == null || caster.unitData == null) continue;

            // 적만
            if (u.unitData.faction == caster.unitData.faction) continue;

            // XZ 평면 기준 원형 판정(높이 차이 무시)
            if (SqrDistXZ(u.transform.position, center) > r2) continue;

            if (hit.Add(u))
                result.Add(u);
        }

        return result;
    }

    private static float SqrDistXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }

    private static void StartOrRestartDelayedBlind(UnitCombatFSM victim, float delay, float blindSec)
    {
        if (victim == null) return;

        if (_pendingBlind.TryGetValue(victim, out var running) && running != null)
        {
            victim.StopCoroutine(running);
        }

        var co = victim.StartCoroutine(CoApplyBlindAfterDelay(victim, delay, blindSec));
        _pendingBlind[victim] = co;
    }

    private static IEnumerator CoApplyBlindAfterDelay(UnitCombatFSM victim, float delay, float blindSec)
    {
        if (victim == null) yield break;

        yield return new WaitForSeconds(delay);

        if (victim != null && victim.IsAlive())
        {
            if (victim.blind != null)
            {
                victim.blind.Apply(blindSec);
            }
        }

        // 정리
        if (victim != null)
        {
            if (_pendingBlind.TryGetValue(victim, out var co) && co == null)
                _pendingBlind.Remove(victim);
            else
                _pendingBlind.Remove(victim);
        }
    }
}

//뼈갑 전위체
public class DefenseAndDamageReductionSelfBuffSkill  : ISkillBehavior
{
    public bool ShouldTrigger(UnitCombatFSM caster, SkillEffect effect)
    {
        if (caster == null) return false;
        return caster.CanUseSkill();
    }

    public UnitCombatFSM FindTarget(UnitCombatFSM caster, SkillEffect effect)
    {
        return caster;
    }

    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        float defenseUpPercent = effect.skillValue;         //방어력
        float damageReductionAdd = effect.skillMaxStack;    //받는 데미지 감소
        float duration = effect.skillDuration;              //지속시간  

        caster.ApplyBuff(BuffStat.Defense, defenseUpPercent, duration, isPercent: true);
        caster.ApplyBuff(BuffStat.DamageReduction, damageReductionAdd, duration, isPercent: false);
    }

    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
}













// --------------------------------------------  스킬 부가 효과들(상태효과) ----------------------------------------

/// <summary>
/// BleedSystem
/// - 역할: 출혈 로직 통합 관리
/// - 효과: 현재 체력 비례로 10% 데미지 최대 3중첩(중첩당 출혈 효과 1초 증가)
/// </summary>
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
                //target.TakeDamage(bleedDmg);
                // 출혈 DOT로 분류해서 UI가 상태이상 피해로 표시 가능
                // 기존 동작(공격자 없음) 유지: attacker = null
                target.TakeDamage(new DamagePayload(bleedDmg, null, DamageKind.Dot_Bleed));

                Debug.Log($"[출혈] {target.name} → {bleedDmg:F1} 출혈 피해 ({status.stack}중첩)");
                yield return new WaitForSeconds(tickTime);
                totalTime += tickTime;
            }
            status.stack--;
            totalTime = 0f;
        }

        activeBleeds.Remove(target);
    }

    public static void RemoveBleed(UnitCombatFSM target)
    {
        if (target == null) return;
        if (activeBleeds.TryGetValue(target, out var status))
        {
            if (status.routine != null)
                target.StopCoroutine(status.routine);
            activeBleeds.Remove(target);
            Debug.Log($"[출혈 해제] {target.name}");
        }
    }
}



/// <summary>
/// SilenceSystem
/// - 역할: 침묵 상태를 통합 관리
/// - 효과: 침묵 중에는 유닛 스킬 사용 불가
/// </summary>
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


/// <summary>
/// BlindSystem
/// - 역할: 실명 상태를 통합 관리 (적용, 지속, 해제, 남은 시간, 이벤트)
/// - 효과: 실명 중엔 UnitCombatFSM.Attack()이 모두 MISS 처리됨(Attack()에서 가드)
/// - blind.Apply(duration)
/// </summary>
[DisallowMultipleComponent]
public class BlindSystem : MonoBehaviour
{
    public enum StackPolicy
    {
        Refresh,    // 새로 적용 시 지속시간 갱신(덮어쓰기)  ← 기본
        Extend,     // 새로 적용 시 지속시간 누적(최대 상한 적용)
    }

    [Header("Blind Settings")]
    [SerializeField] private StackPolicy stackPolicy = StackPolicy.Refresh;
    [SerializeField] private float maxDurationCap = 10f; // Extend일 때 누적 상한(원하면 0으로 꺼도 됨)

    public bool IsBlinded => _isBlinded;
    public float Remaining => _remaining;

    public event Action<bool> OnBlindStateChanged; // 인게임 UI/아이콘 갱신용

    private bool _isBlinded;
    private float _remaining;
    private Coroutine _co;

    /// <summary>
    /// 실명 적용. duration초 동안 블라인드 유지(정책에 따라 새 적용 시 갱신/누적).
    /// </summary>
    public void Apply(float durationSeconds)
    {
        if (durationSeconds <= 0f) return;

        if (_isBlinded)
        {
            switch (stackPolicy)
            {
                case StackPolicy.Refresh:
                    _remaining = durationSeconds;
                    break;
                case StackPolicy.Extend:
                    _remaining += durationSeconds;
                    if (maxDurationCap > 0f) _remaining = Mathf.Min(_remaining, maxDurationCap);
                    break;
            }
        }
        else
        {
            _isBlinded = true;
            _remaining = durationSeconds;
            OnBlindStateChanged?.Invoke(true);
            _co = StartCoroutine(CoTick());
        }
    }

    /// <summary>
    /// 강제 해제(즉시).
    /// </summary>
    public void Clear()
    {
        if (_co != null) { StopCoroutine(_co); _co = null; }
        if (_isBlinded)
        {
            _isBlinded = false;
            _remaining = 0f;
            OnBlindStateChanged?.Invoke(false);
        }
    }

    private IEnumerator CoTick()
    {
        while (_remaining > 0f)
        {
            _remaining -= Time.deltaTime; // 게임 시간 기준(배속/슬로모 반영)
            yield return null;
        }
        _co = null;
        _isBlinded = false;
        _remaining = 0f;
        OnBlindStateChanged?.Invoke(false);
    }

    private void OnDisable()
    {
        // 오브젝트 비활성/파괴 시 안전 해제
        if (_co != null) { StopCoroutine(_co); _co = null; }
        _isBlinded = false;
        _remaining = 0f;
    }
}



/// <summary>
/// StunSystem
/// - 역할: 기절 상태를 통합 관리(적용, 지속, 해제, 중첩/갱신 정책)
/// - 효과: 기절 중엔 이동 불가(NavMeshAgent 정지). IsStunned 가드로 공격/스킬 시도 차단 권장.
/// - API:
///   StunSystem.Apply(UnitCombatFSM target, float duration)   // 갱신 정책: 새로 적용 시 남은 시간보다 길면 갱신(RefreshMax)
///   bool IsStunned { get; }
/// </summary>
[DisallowMultipleComponent]
public class StunSystem : MonoBehaviour
{
    public enum StackPolicy { RefreshMax, Additive /* 필요 시 확장 */ }

    private float remain;
    private Coroutine routine;
    private NavMeshAgent agent;
    private float prevSpeed;
    private bool prevStopped;

    public bool IsStunned => remain > 0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public static void Apply(UnitCombatFSM target, float duration, StackPolicy policy = StackPolicy.RefreshMax)
    {
        if (target == null || !target.IsAlive()) return;

        var stun = target.GetComponent<StunSystem>();
        if (stun == null) stun = target.gameObject.AddComponent<StunSystem>();
        stun.ApplyInternal(duration, policy);
    }

    private void ApplyInternal(float duration, StackPolicy policy)
    {
        if (duration <= 0f) return;

        switch (policy)
        {
            case StackPolicy.RefreshMax:
                remain = Mathf.Max(remain, duration);
                break;
            case StackPolicy.Additive:
                remain += duration;
                break;
        }

        if (routine == null) routine = StartCoroutine(CoStun());
    }

    private IEnumerator CoStun()
    {
        // 시작: 이동 정지
        if (agent != null)
        {
            prevSpeed = agent.speed;
            prevStopped = agent.isStopped;
            agent.isStopped = true;
            agent.speed = 0f;
        }

        // (선택) 애니/이펙트 트리거 가능

        while (remain > 0f)
        {
            remain -= Time.deltaTime;
            yield return null;
        }

        // 종료: 이동 복구
        if (agent != null)
        {
            agent.isStopped = prevStopped;
            agent.speed = prevSpeed;
        }

        routine = null;
    }

    public void ForceClear()
    {
        remain = 0f;
        if (routine != null) StopCoroutine(routine);
        routine = null;
        if (agent != null)
        {
            agent.isStopped = prevStopped;
            agent.speed = prevSpeed;
        }
    }
}

/// <summary>
/// RegenStatus : 이로운 상태효과(주기적 회복)
/// - StartPulse(interval, amount, isPercent, duration=0) : interval마다 회복
///   isPercent=true면 최대체력*amount, false면 절대값 amount.
///   duration<=0이면 무한 지속(패시브에 적합).
/// </summary>
[DisallowMultipleComponent]
public class RegenStatus : MonoBehaviour
{
    private Coroutine pulse;
    private UnitCombatFSM unit;

    void Awake() => unit = GetComponent<UnitCombatFSM>();

    public void StartPulse(float interval, float amount, bool isPercent, float duration = 0f)
    {
        StopPulse();
        pulse = StartCoroutine(CoPulse(interval, amount, isPercent, duration));
    }

    public void StopPulse()
    {
        if (pulse != null) StopCoroutine(pulse);
        pulse = null;
    }

    public void ClearAll() => StopPulse();

    private IEnumerator CoPulse(float interval, float amount, bool isPercent, float duration)
    {
        if (unit == null) yield break;
        float t = 0f;

        // 첫 틱을 지연 없이 줄지 여부는 기획에 맞게 선택
        while (unit != null && unit.IsAlive())
        {
            if (duration > 0f && t >= duration) break;

            yield return new WaitForSeconds(interval);
            t += interval;

            if (unit == null || !unit.IsAlive()) break;

            float heal = isPercent ? unit.stats.health * amount : amount;
            unit.ReceiveHealing(heal); // 내부에서 Max HP로 클램프됨
        }
        pulse = null;
    }
}


/// <summary>
/// DeferredSelfDamageStatus : 해로운 상태효과(자기 자신에게 지연 피해)
/// - AddSplitDamage(totalDamage, ticks, duration)
///   duration 동안 ticks회 균등 분배. OnBeforeTakeDamage로 다시 분할되지 않도록 attacker=null로 전달.
/// </summary>
[DisallowMultipleComponent]
public class DeferredSelfDamageStatus : MonoBehaviour
{
    private UnitCombatFSM unit;

    void Awake() => unit = GetComponent<UnitCombatFSM>();

    public void AddSplitDamage(float totalDamage, int ticks, float duration)
    {
        if (unit == null || !unit.IsAlive()) return;
        if (totalDamage <= 0f || ticks <= 0 || duration <= 0f) return;
        StartCoroutine(CoSplit(totalDamage, ticks, duration));
    }

    public void ClearAll()
    {
        StopAllCoroutines();
    }

    private IEnumerator CoSplit(float total, int ticks, float duration)
    {
        float perTick = total / ticks;
        float interval = duration / ticks;
        for (int i = 0; i < ticks; i++)
        {
            yield return new WaitForSeconds(interval);
            if (unit == null || !unit.IsAlive()) yield break;

            // 내부 지연피해는 다시 70/30으로 분할되지 않도록 attacker=null로 보냄
            unit.TakeDamage(perTick, null);
        }
    }
}

//중독 상태
public static class PoisonSystem
{
    private class PoisonStatus
    {
        public int stack;            // 1~2
        public int ticksRemaining;   // 남은 틱 횟수(최대 6)
        public Coroutine routine;

        public UnitCombatFSM source;       // 누가 걸었는지(킬 크레딧/로그용)
        public float percentPerTickBase;   // 틱당MaxHP 비율 (여기선 0.05)
    }

    private static readonly Dictionary<UnitCombatFSM, PoisonStatus> activePoisons = new();

    /// <summary>
    /// 중독 적용 규칙(요구사항 반영):
    /// - stack은 최대 2
    /// - 적용될 때마다 ticksRemaining은 무조건 6으로 리프레시
    /// - 1중첩 상태에서 다시 맞으면 2중첩이 되며 ticksRemaining=6으로 리프레시
    /// - 2중첩 상태에서 또 맞아도 stack=2 유지, ticksRemaining=6으로 리프레시
    /// </summary>
    public static void ApplyPoison(
        UnitCombatFSM target,
        UnitCombatFSM source,
        float percentPerTick = 0.05f,
        int maxStack = 2,
        int totalTicks = 6
    )
    {
        if (target == null || !target.IsAlive()) return;

        maxStack = Mathf.Max(1, maxStack);        // 안전
        totalTicks = Mathf.Max(1, totalTicks);
        percentPerTick = Mathf.Max(0f, percentPerTick);

        if (!activePoisons.TryGetValue(target, out var status))
        {
            status = new PoisonStatus();
            activePoisons[target] = status;

            status.stack = 1;
            status.ticksRemaining = totalTicks;    //처음 걸릴 때도 6틱 시작
            status.source = source;
            status.percentPerTickBase = percentPerTick;

            status.routine = target.StartCoroutine(PoisonRoutine(target, status));
        }
        else
        {
            //스택은 최대 2, 그리고 "항상" 6틱으로 리프레시
            status.stack = Mathf.Min(status.stack + 1, maxStack);
            status.ticksRemaining = totalTicks;

            // 적용자/수치 갱신(원하면 최초 적용자 유지로 바꿀 수 있음)
            status.source = source;
            status.percentPerTickBase = percentPerTick;
        }
    }

    private static IEnumerator PoisonRoutine(UnitCombatFSM target, PoisonStatus status)
    {
        const float interval = 1f; //1초에 1번 = 6초 동안 총 6틱

        while (target != null && target.IsAlive())
        {
            // 남은 틱이 없으면 종료
            if (status.ticksRemaining <= 0)
                break;

            // 1초 기다렸다가 틱 적용 (6틱 = 6초)
            yield return new WaitForSeconds(interval);

            if (target == null || !target.IsAlive())
                yield break;

            if (status.ticksRemaining <= 0)
                break;

            float curHp = target.currentHP;

            //  틱당 5% * 스택(1~2)
            float dmgThisTick = (curHp * status.percentPerTickBase) * status.stack;

            if (dmgThisTick > 0f)
            {
                // 여기를 source로 넘기면,피흡(가한 피해의 20%)같은 패시브가
                //     독 틱에도 반응할 수 있음(원하지 않으면 null로 바꿔야 함).
                target.TakeDamage(new DamagePayload(dmgThisTick, status.source, DamageKind.Dot_Poison));
                // target.TakeDamage(dmgThisTick, null);
            }

            status.ticksRemaining--;
        }

        // 정리
        if (target != null && activePoisons.TryGetValue(target, out var cur) && cur == status)
            activePoisons.Remove(target);
    }

    public static void RemovePoison(UnitCombatFSM target)
    {
        if (target == null) return;

        if (activePoisons.TryGetValue(target, out var status))
        {
            if (status.routine != null)
                target.StopCoroutine(status.routine);

            activePoisons.Remove(target);
        }
    }
}


public static class StatusCleanseUtil
{
    /// <summary>
    /// 해로운 상태이상/디버프 정화
    /// - 스탯감소 디버프는 UnitCombatFSM_DebuffRegistry.ApplyStatDebuffTracked로 적용된 것만 정화 가능
    /// </summary>
    public static void CleanseHarmful(
        UnitCombatFSM u,
        bool clearStun,
        bool clearSilence,
        bool clearBlind,
        bool clearBleed,
        bool clearPoison,
        bool clearStatDebuffs)
    {
        if (u == null || !u.IsAlive()) return;

        // 침묵
        if (clearSilence)
            u.isSilenced = false;

        // 기절
        if (clearStun && u.TryGetComponent<StunSystem>(out var stun))
            stun.ForceClear();

        // 실명
        if (clearBlind && u.blind != null)
            u.blind.Clear();

        // 출혈
        if (clearBleed)
            BleedSystem.RemoveBleed(u);

        // 중독
        if (clearPoison)
            PoisonSystem.RemovePoison(u);

        // 스탯 감소 디버프(추적형)
        if (clearStatDebuffs)
            UnitCombatFSM.UnitCombatFSM_DebuffRegistry.CleanseAllStatDebuffs(u);
    }
}

















//----------------------------------------------------------스킬 디버그 라인(사거리 및 범위)--------------------------------------------------------------

/// <summary>
/// 버프지대 로직:
/// - 매 0.1초(기본)마다 반경 내 '같은 진영' 아군을 스캔.
/// - 처음 진입한 유닛엔 3종 버프(공/공속/이속)를 퍼센트로 적용(ModifyStat, isPercent=true).
/// - 반경에서 벗어난 유닛은 즉시 해제(ModifyStat, isRemove=true).
/// - 지속이 끝나면 남아있는 모든 유닛의 버프를 일괄 해제하고 파괴.
/// - 간단한 원형 표시(LineRenderer)로 위치/범위 확인 가능.
/// //증기 화력 관제사
/// </summary>
public class BuffZoneController : MonoBehaviour
{
    private UnitCombatFSM owner; // 시전자 (진영 판정 용도)
    private float radius;
    private float duration;
    private float atkPct, asPct, msPct;
    private float tick = 0.1f;

    // 현재 버프 적용 중인 유닛 추적
    private readonly HashSet<UnitCombatFSM> buffed = new();

    // 간단 표시
    private LineRenderer circle;

    public void Initialize(UnitCombatFSM owner, float radius, float duration, float atkPct, float asPct, float msPct)
    {
        this.owner = owner;
        this.radius = radius;
        this.duration = duration;
        this.atkPct = atkPct;
        this.asPct = asPct;
        this.msPct = msPct;

        SetupCircle();
        StartCoroutine(CoZoneLoop());
    }

    private void SetupCircle()
    {
        circle = gameObject.AddComponent<LineRenderer>();
        circle.positionCount = 64 + 1;
        circle.useWorldSpace = true;
        circle.widthMultiplier = 0.05f;
        // 머티리얼/컬러는 기본값 사용(임시 표시 목적)

        // 첫 그리기
        DrawCircle();
    }

    private void DrawCircle()
    {
        if (circle == null) return;

        Vector3 c = transform.position;
        for (int i = 0; i <= 64; i++)
        {
            float t = i / 64f;
            float ang = t * Mathf.PI * 2f;
            var pos = new Vector3(Mathf.Cos(ang) * radius, 0.05f, Mathf.Sin(ang) * radius);
            circle.SetPosition(i, c + pos);
        }
    }

    private IEnumerator CoZoneLoop()
    {
        float t = 0f;

        while (t < duration && owner != null)
        {
            // 1) 현재 장면의 모든 유닛 중, 같은 진영 + 생존 + 반경 내
            var all = GameObject.FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None);
            var center = transform.position;
            for (int i = 0; i < all.Length; i++)
            {
                var u = all[i];
                if (u == null || !u.IsAlive()) continue;
                if (u.unitData.faction != owner.unitData.faction) continue;

                float d = Vector3.Distance(u.transform.position, center);
                bool inside = d <= radius;

                if (inside)
                {
                    // 신규 진입 → 버프 적용
                    if (!buffed.Contains(u))
                    {
                        ApplyBuffs(u);
                        buffed.Add(u);
                    }
                }
            }

            // 2) 반경 밖으로 나간 유닛 정리
            //    (집합을 복사해서 순회해야 안전)
            if (buffed.Count > 0)
            {
                var toRemove = new List<UnitCombatFSM>();
                foreach (var u in buffed)
                {
                    if (u == null || !u.IsAlive())
                    {
                        toRemove.Add(u);
                        continue;
                    }

                    float d = Vector3.Distance(u.transform.position, center);
                    if (d > radius || u.unitData.faction != owner.unitData.faction)
                        toRemove.Add(u);
                }

                for (int k = 0; k < toRemove.Count; k++)
                {
                    var u = toRemove[k];
                    RemoveBuffs(u);
                    buffed.Remove(u);
                }
            }

            // 3) 라인 업데이트(혹시 중심을 움직였을 경우 대비)
            DrawCircle();

            yield return new WaitForSeconds(tick);
            t += tick;
        }

        // 지속 종료: 잔여 버프 일괄 해제
        foreach (var u in buffed)
            if (u != null) RemoveBuffs(u);
        buffed.Clear();

        Destroy(gameObject);
    }

    private void ApplyBuffs(UnitCombatFSM u)
    {
        // 퍼센트 버프(ModifyStat 내부에서 공격/공속은 곱셈, 이속은 agent.speed 동기화됨)
        u.ModifyStat(BuffStat.Attack, atkPct, true, false);
        u.ModifyStat(BuffStat.AttackSpeed, asPct, true, false);
        u.ModifyStat(BuffStat.MoveSpeed, msPct, true, false);
        // 필요시 디버그
        // Debug.Log($"[ZoneBuff] + {u.name} atk+{atkPct:P0} as+{asPct:P0} ms+{msPct:P0}");
    }

    private void RemoveBuffs(UnitCombatFSM u)
    {
        u.ModifyStat(BuffStat.Attack, atkPct, true, true);
        u.ModifyStat(BuffStat.AttackSpeed, asPct, true, true);
        u.ModifyStat(BuffStat.MoveSpeed, msPct, true, true);
        // Debug.Log($"[ZoneBuff] - {u.name}");
    }
}



/// <summary>
/// 직사각형 AoE를 간단 라인으로 1회 그려주는 디버그 표시.
/// - center, dir, L, W를 받아 4변을 LineRenderer로 그림.
/// - lifeSeconds 뒤 자동 파괴.
/// //EMP 방출자
/// </summary>
public class RectAoeDebug : MonoBehaviour
{
    public static float yOffset = 0.02f;   // 지면에 살짝 띄워 보이게

    public static void Spawn(Vector3 center, Vector3 dir, float L, float W, float lifeSeconds = 0.6f, int edgeSubdiv = 1)
    {
        var go = new GameObject("RectAoeDebug");
        var d = go.AddComponent<RectAoeDebug>();
        d.Init(center, dir, L, W, lifeSeconds, edgeSubdiv);
    }

    private LineRenderer lr;

    private void Init(Vector3 center, Vector3 dir, float L, float W, float lifeSeconds, int edgeSubdiv)
    {
        transform.position = center;

        lr = gameObject.AddComponent<LineRenderer>();
        lr.positionCount = (edgeSubdiv * 4) + 1; // 사각형 네 변 분할 + 폐합점
        lr.widthMultiplier = 0.04f;
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;

        // 전방/우측 벡터
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward;
        else dir.Normalize();
        Vector3 right = new Vector3(dir.z, 0f, -dir.x);

        Vector3 halfF = dir * (L * 0.5f);
        Vector3 halfR = right * (W * 0.5f);

        // 사각형 꼭짓점
        Vector3 p0 = center - halfF - halfR;
        Vector3 p1 = center - halfF + halfR;
        Vector3 p2 = center + halfF + halfR;
        Vector3 p3 = center + halfF - halfR;

        // 변마다 edgeSubdiv로 세분하여 지면에 스냅
        int idx = 0;
        WriteEdge(p0, p1, edgeSubdiv, ref idx);
        WriteEdge(p1, p2, edgeSubdiv, ref idx);
        WriteEdge(p2, p3, edgeSubdiv, ref idx);
        WriteEdge(p3, p0, edgeSubdiv, ref idx);
        lr.SetPosition(idx, lr.GetPosition(0)); // 폐합

        StartCoroutine(CoLife(lifeSeconds));
    }

    private void WriteEdge(Vector3 a, Vector3 b, int subdiv, ref int idx)
    {
        for (int i = 0; i < subdiv; i++)
        {
            float t = (float)i / subdiv;
            Vector3 p = Vector3.Lerp(a, b, t);
            p.y = SampleGroundY(p) + yOffset;
            lr.SetPosition(idx++, p);
        }
    }

    private static float SampleGroundY(Vector3 pos)
    {
        // 1) NavMesh 우선 (NavMeshAgent를 쓰는 프로젝트라면 바닥 높이와 가장 일치)
        if (NavMesh.SamplePosition(pos, out var nh, 10f, NavMesh.AllAreas))
            return nh.position.y;

        // 2) Terrain이 있다면 Terrain 높이 사용
        if (Terrain.activeTerrain != null)
        {
            float h = Terrain.activeTerrain.SampleHeight(pos);
            return h + Terrain.activeTerrain.transform.position.y;
        }

        // 3) 단순 Raycast (레이어마스크 없이 기본 오버로드)
        if (Physics.Raycast(pos + Vector3.up * 50f, Vector3.down, out var hit, 200f))
            return hit.point.y;

        // 4) 실패하면 원래 y 유지
        return pos.y;
    }

    private IEnumerator CoLife(float t)
    {
        yield return new WaitForSeconds(t);
        Destroy(gameObject);
    }
}