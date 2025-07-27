using UnityEngine;
using UnityEngine.AI;
using BattleTypes.Enums;
using UnitSkillTypes.Enums;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class AppliedPassiveEffect
{
    public UnitSkillType skillType;
    public float value;
    public UnitCombatFSM source;
    
}
public enum BuffStat
{
    None,
    Attack,
    Defense,
    MoveSpeed,
    AttackSpeed,
    Health,
    AttackDistance,
    DamageReduction,
    CriticalChance,
}

public partial class UnitCombatFSM : MonoBehaviour
{
    public UnitCardData unitData; // 원본 ScriptableObjcet
    public NavMeshAgent agent;
    public UnitCombatFSM targetEnemy; //현재 타겟 Enemy 
    [HideInInspector] public float attackTimer;
    public float currentHP;
    public float criticalMultiplier = 1.5f;
    public float skillTimer = 0f; // 스킬 쿨다운 누적 
    public SkillData skillData;
    public UnitCombatFSM targetAlly; //힐 버프 대상 
    public System.Action OnPostAttack;


    public SkillExecutor skillExecutor = new SkillExecutor();
    private UnitState currentState;
    public RuntimeUnitStats stats; // 복사된 인스턴스 스텟 

    public bool isProcessingSkill = false; // 중복 상태 전환 방지용 


    public List<AppliedPassiveEffect> activePassiveEffects = new List<AppliedPassiveEffect>();

    public Action<float, UnitCombatFSM> OnReflectDamage;


    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }
    void Start()
    {
        CreateRangeIndicator(); //런타임 사거리

        CloneStats(); // 스탯 복사 
        currentHP = stats.health;
        agent.speed = stats.moveSpeed;
        agent.stoppingDistance = stats.attackDistance * 5;

        ChangeState(new IdleState(this));

        // 패시브 스킬 
        StartCoroutine(ApplyPassiveEffectsDelayed());

    }

    public void OnDeath()
    {
        RemovePassiveEffects(); // 패시브 해제
        OnReflectDamage = null; // 💥 반사 효과도 제거

        if (skillData != null && skillData.effects != null)
        {
            foreach (var effect in skillData.effects)
            {
                var behavior = SkillExecutor.GetSkillBehavior(effect.skillType);
                behavior?.Remove(this, effect);
            }
        }
    }

    void Update()
    {
        skillTimer += Time.deltaTime; // 스킬 쿨타이머 

        // 스킬 우선 타겟 전환 체크
        if (!isProcessingSkill && skillData != null && skillExecutor.ShouldMoveToSkillTarget(this, skillData))
        {
            isProcessingSkill = true;
            agent.ResetPath();
            ChangeState(new MoveState(this, true)); // true = 아군 타겟팅
            return;
        }

        // 기존 Idle 상태일 때 바로 발동 (즉시 거리 안에 있는 경우)
        if (currentState is IdleState && !isProcessingSkill && skillData != null)
        {
            if (TryUseSkill()) return;
        }


        if (targetEnemy == null || !targetEnemy.IsAlive())
            FindNewTarget();

        if (currentState is IdleState && !isProcessingSkill && skillData != null)
        {
            if (skillExecutor.TryUseSkillIfPossible(this, skillData))
                return; // 스킬이 발동되면 그 턴은 종료
        }

        
        currentState?.Update();


        if (!IsAlive() && !(currentState is DeadState))
        {
            ChangeState(new DeadState(this));
        }

        if (rangeIndicator != null)
        {
            UpdateRangeIndicator();
        }  
    }

    

    public bool TryUseSkill() // 기존 FSM 상태에서 이 메서드만 호출하면 됨
    {
        return skillExecutor.TryUseSkillIfPossible(this, this.skillData);
    }

    // private bool IsSelfTriggerSkill()
    // {
    //     //자기 자신 조건부 발동 스킬 타입 
    //     if (skillData == null || skillData.effects == null || skillData.effects.Count == 0) return false;
    //     var effect = skillData.effects[0];
    //     return effect.skillType == UnitSkillType.BarrierOnHpHalf;
    // }


    // public void FaceTarget(UnitCombatFSM target)
    // {
    //     Vector3 dir = target.transform.position - transform.position;
    //     dir.y = 0f;
    //     if (dir != Vector3.zero)
    //         transform.forward = dir.normalized;
        
    // }

    public void ChangeState(UnitState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }
    public bool IsAlive()
    {
        return currentHP > 0;
    }

    public void Attack()
    {
        if (targetEnemy == null || !targetEnemy.IsAlive()) return;
        float baseDamage = stats.attack;
        //치명타 판정 
        bool isCritical = UnityEngine.Random.value < stats.criticalChance;

        if (isCritical)
        {
            baseDamage *= criticalMultiplier; // 치명타 배율 적용 
            Debug.Log($"[Critical] {gameObject.name} → 치명타!");
        }

        targetEnemy.TakeDamage(baseDamage, this); // 공격자 자신 전달
        
        //후처리용 이벤트 :추가 타격, 버프, 출혈 등 모든 후처리를 이곳에서 수행 가능 
        OnPostAttack?.Invoke();
    }

    public void TakeDamage(float damage, UnitCombatFSM attacker = null)
    {
        if (stats.guardCount > 0)
        {
            stats.guardCount--;
            Debug.Log($"[가드] {gameObject.name} -> 피격 방어 남은 가드: {stats.guardCount}");
            return;
        }

        //Clamp 처리: damageReduction이 1.0 이상이면 최소 0, 음수면 최대 1
        float reductionFactor = Mathf.Clamp01(1.0f - stats.damageReduction);
        float effectiveDamage = damage * (100f / (100f + stats.defense));
        effectiveDamage *= reductionFactor;


        // 방어막 우선 차감 
        if (stats.barrier > 0f)
        {
            float shieldAbsorb = Math.Min(stats.barrier, effectiveDamage);
            stats.barrier -= shieldAbsorb;
            effectiveDamage -= shieldAbsorb;
            Debug.Log($"[방어막 차감] {gameObject.name}: {shieldAbsorb}만큼 흡수, 남은 방어막: {stats.barrier}");
        }
        if (effectiveDamage > 0)
        {
            currentHP -= effectiveDamage;
        }

        Debug.Log($"[피격] {name} - 받은 데미지: {effectiveDamage:F1} / 남은 HP: {currentHP:F1}");

        if (currentHP <= 0)
        {
            ChangeState(new DeadState(this));
        }
        
        // 데미지 반사 처리
        if (attacker != null && OnReflectDamage != null)
        {
            Debug.Log($"[Reflect] 반사 발동 - {this.name} ← {attacker.name}");
            OnReflectDamage.Invoke(effectiveDamage, attacker);
        }
    }

    // 급속 파열기 헬퍼 List
    public List<UnitCombatFSM> FindEnemiesInCone(float angleDeg, float rangeMultiplier)
    {
        List<UnitCombatFSM> targets = new();
        float radius = stats.attackDistance * rangeMultiplier;
        Vector3 forward = transform.forward;

        foreach (var enemy in FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None))
        {
            if (enemy == this || !enemy.IsAlive()) continue;
            if (enemy.unitData.faction == this.unitData.faction) continue;

            Vector3 dir = enemy.transform.position - transform.position;
            float dist = dir.magnitude;
            float angle = Vector3.Angle(forward, dir);

            //Debug.Log($"[ConeCheck] 대상: {enemy.name}, 거리: {dist:F2}, 각도: {angle:F1}");
            if (dist <= radius && angle <= angleDeg * 0.5f)
            {
                targets.Add(enemy);
            }
        }

        return targets;
    }

    public void FindNewTarget()
    {
        float shortestDistance = Mathf.Infinity;
        UnitCombatFSM nearestEnemy = null;

        var all = FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None);
        foreach (var unit in all)
        {
            if (unit == this || !unit.IsAlive()) continue;
            if (unit.unitData.faction == unitData.faction) continue;

            float dist = Vector3.Distance(transform.position, unit.transform.position);
            if (dist < shortestDistance)
            {
                shortestDistance = dist;
                nearestEnemy = unit;
            }
        }

        targetEnemy = nearestEnemy;
    }


    private void CloneStats()
    {
        stats = new RuntimeUnitStats
        {
            health = unitData.health,
            moveSpeed = unitData.moveSpeed,
            attack = unitData.attack,
            defense = unitData.defense,
            attackSpeed = unitData.attackSpeed,
            attackDistance = unitData.attackDistance,
            criticalChance = unitData.battleType switch
            {
                BattleType.Melee => 0.1f,
                BattleType.Ranged => 0.3f,
                BattleType.Support => 0.05f,
                _ => 0.1f
            }
        };

    }

    //이전 치명타 배율 
    // private void AssignCriticalChance()
    // {
    //     switch (unitData.battleType)
    //     {
    //         case BattleType.Melee:
    //             criticalChance = 0.1f;
    //             break;
    //         case BattleType.Ranged:
    //             criticalChance = 0.3f;
    //             break;
    //         case BattleType.Support:
    //             criticalChance = 0.05f;
    //             break;
    //         default:
    //             criticalChance = 0.1f;
    //             break;
    //     }
    // }

    public UnitCombatFSM FindNearestEnemy()
    {
        float min = float.MaxValue;
        UnitCombatFSM result = null;

        foreach (var unit in FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None))
        {
            if (unit == this || !unit.IsAlive()) continue;
            if (unit.unitData.faction == this.unitData.faction) continue; // 같은 진영 제외 

            float d = Vector3.Distance(transform.position, unit.transform.position);
            if (d < min)
            {
                min = d;
                result = unit;
            }
        }
        return result;
    }

    public UnitCombatFSM FindNearestAlly()
    {
        float min = float.MaxValue;
        UnitCombatFSM result = null;

        foreach (var unit in FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None))
        {
            if (unit == this || !unit.IsAlive()) continue;
            if (unit.unitData.faction != this.unitData.faction) continue;

            float d = Vector3.Distance(transform.position, unit.transform.position);
            if (d < min)
            {
                min = d;
                result = unit;
            }
        }

        return result;
    }

    public UnitCombatFSM FindLowestHpAlly()
    {
        float minRatio = float.MaxValue;
        UnitCombatFSM result = null;

        foreach (var unit in FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None))
        {
            if (unit == this || !unit.IsAlive()) continue;
            if (unit.unitData.faction != this.unitData.faction) continue;

            float ratio = unit.currentHP / unit.stats.health; // 현재 HP 비율
            if (ratio < minRatio)
            {
                minRatio = ratio;
                result = unit;
            }
        }

        return result;
    }

    public void ReceiveHealing(float amount)
    {
        currentHP += amount;

        if (currentHP > stats.health) currentHP = stats.health;
        Debug.Log($"[회복] {gameObject.name} → {amount} 회복 / 현재 HP: {currentHP:F1}");
    }

    public void ApplyBuff(BuffStat stat, float amount, float duration, bool isPercent = false)
    {
        StartCoroutine(BuffRoutine(stat, amount, duration, isPercent));
    }

    public void ApplyDebuff(BuffStat stat, float amount, float duration)
    {
        StartCoroutine(DebuffRoutine(stat, amount, duration));
    }

    //버프 
    private IEnumerator BuffRoutine(BuffStat stat, float value, float duration, bool isPercent)
    {
        ModifyStat(stat, value, isPercent, false);   // 적용
        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
            ModifyStat(stat, value, isPercent, true); // 해제
        }
    }

    //디버프 
    private IEnumerator DebuffRoutine(BuffStat stat, float amount, float duration)
    {
        ModifyStat(stat, -amount); // 스탯 감소
        yield return new WaitForSeconds(duration);
        ModifyStat(stat, amount); // 스탯 복구
    }

    // private void ModifyStat(BuffStat stat, float value)
    // {
    //     switch (stat)
    //     {
    //         case BuffStat.Attack:
    //             stats.attack += value;
    //             break;
    //         case BuffStat.Defense:
    //             stats.defense += value;
    //             break;
    //         case BuffStat.MoveSpeed:
    //             stats.moveSpeed += value;
    //             agent.speed = stats.moveSpeed; //NavMeshAgent에도 적용 
    //             break;
    //         case BuffStat.AttackSpeed:
    //             stats.attackSpeed += value;
    //             break;
    //     }
    // }


    //-------------------------------------------스킬-----------------------------------------------
    // public void TryUseSkill()
    // {
    //     if (!CanUseSkill() || skillData == null || skillData.effects == null) return;
        
    //     foreach (var effect in skillData.effects)
    //     {
    //         UnitCombatFSM skillTarget = null;
    //         switch (effect.skillType)
    //         {
    //             case UnitSkillType.InstantHeal:
    //                 skillTarget = FindLowestHpAlly();
    //                 break;
    //             case UnitSkillType.IncreaseAttack:
    //                 skillTarget = FindNearestAlly();
    //                 break;
    //             case UnitSkillType.AttackDown:
    //                 skillTarget = FindNearestEnemy();
    //                 break;
    //             case UnitSkillType.MultiHit:
    //                 skillTarget = FindNearestEnemy();
    //                 break;
    //             case UnitSkillType.BarrierOnHpHalf:
    //                 skillTarget = this; // 자기 자신 적용 
    //                 isProcessingSkill = false;
    //                 break;
    //             case UnitSkillType.DashAttackAndGuard:
    //                 skillTarget = FindNearestEnemy();
    //                 break;
    //             case UnitSkillType.ThrowSpearAttack:
    //                 skillTarget = FindNearestEnemy();
    //                 break;
    //                 // 기타 스킬타입 분기 추가
    //         }
    //         skillExecutor.ExecuteSkill(skillData, this, skillTarget);
    //     }
    //     skillTimer = 0f;
    // }
    
    //스킬 쿨타임 조건 확인 
    public bool CanUseSkill()
    {
        return skillData != null && skillTimer >= skillData.skillCoolDown;
    }

    //스킬 조건 진입 (ShouldUseSKill → TryUseSkill)
    // public bool ShouldUseSkill()
    // {
    //     if (!CanUseSkill() || skillData == null || skillData.effects == null || skillData.effects.Count == 0)
    //         return false;

    //     // 단일 효과만 있다고 가정
    //     var effect = skillData.effects[0];

    //     switch (effect.skillType)
    //     {
    //         case UnitSkillType.InstantHeal:
    //             targetAlly = FindLowestHpAlly();
    //             return targetAlly != null && targetAlly.currentHP < targetAlly.stats.health;
    //         case UnitSkillType.IncreaseAttack:
    //             targetAlly = FindNearestAlly();
    //             return targetAlly != null;
    //         case UnitSkillType.AttackDown:
    //             targetEnemy = FindNearestEnemy();
    //             return targetEnemy != null;
    //         case UnitSkillType.MultiHit:
    //             targetEnemy = FindNearestEnemy();
    //             return targetEnemy != null;
    //         case UnitSkillType.BarrierOnHpHalf:
    //             //50% 이하 및 방어막 없을 때 발동 
    //             return currentHP / stats.health <= 0.5f && stats.barrier <= 0.01f;
    //         case UnitSkillType.DashAttackAndGuard:
    //             targetEnemy = FindNearestEnemy();
    //             return targetEnemy != null;
    //         case UnitSkillType.ThrowSpearAttack:
    //             targetEnemy = FindNearestEnemy();
    //             if (targetEnemy != null)
    //             {
    //                 float distance = Vector3.Distance(transform.position, targetEnemy.transform.position);
    //                 float maxRange = stats.attackDistance * 6f;
    //                  Debug.Log($"[창투척 조건] 타겟: {targetEnemy.name}, 거리: {distance:F2}, 허용범위: {maxRange:F2}");
    //                 return distance <= maxRange;
    //             }
    //             Debug.LogWarning("[창투척 조건] 타겟 없음");

    //             return false;
    //         default:
    //             return false;
    //     }
    // }


    // ----- [조건부 버프 및 스킬 : 효과 적용/해제 함수맵] -----
 
    private static readonly Dictionary<UnitSkillType, System.Action<UnitCombatFSM, SkillEffect>> applyEffectMap =
        new Dictionary<UnitSkillType, System.Action<UnitCombatFSM, SkillEffect>>()
    {
        // 근접형 아군 전체 방어력 5% 증가 (자기 자신 포함, 모든 근접 아군)/ 기어 방패병
        { UnitSkillType.IncreaseDefense, (unit, effect) => {
            foreach (var ally in GameObject.FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None))
            {
                if (!ally.IsAlive()) continue;
                if (ally.unitData.faction != unit.unitData.faction) continue;
                if (ally.unitData.battleType != BattleType.Melee) continue;
                float baseDefense = ally.unitData.defense; // 또는 별도 baseDefense 필드 활용
                float addValue = baseDefense * effect.skillValue; // 원본 기준
                ally.stats.defense += addValue;
                ally.activePassiveEffects.Add(new AppliedPassiveEffect { skillType = effect.skillType, value = addValue, source = unit });
            }
        }},
        
        //지연 발동 버프 / 하이브리드 기병병
        { UnitSkillType.DelayBuff, (unit, effect) => {
            unit.StartCoroutine(DelayedBuffRoutine(unit, effect));
        }},
        
        
        // DoubleAttackSkill
        { UnitSkillType.DoubleAttack, (unit, effect) =>
            {
                var behavior = new DoubleAttackSkill();
                behavior.Execute(unit, null, effect);
            }
        },
        // GrowBuffOverTime 
        { UnitSkillType.GrowBuffOverTime, (unit, effect) =>
            {
                new GrowBuffOverTimeSkill().Execute(unit, null, effect);
            }
        },
        { UnitSkillType.PassiveAreaBuff, (unit, effect) =>
            {
                new PassiveAreaBuffSkill().Execute(unit, null, effect);
            }
        },
        // 신규 효과는 여기만 추가

        };

    private static readonly Dictionary<UnitSkillType, System.Action<UnitCombatFSM, SkillEffect>> removeEffectMap =
        new Dictionary<UnitSkillType, System.Action<UnitCombatFSM, SkillEffect>>()
    {
        // 근접형 아군 전체 방어력 5% 증가 해제 (자기 자신 포함, 모든 근접 아군)/ 기어 방패병
        { UnitSkillType.IncreaseDefense, (unit, effect) => {
            foreach (var ally in GameObject.FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None))
            {
                if (!ally.IsAlive()) continue;
                if (ally.unitData.faction != unit.unitData.faction) continue;
                if (ally.unitData.battleType != BattleType.Melee) continue;
                var targetEffect = ally.activePassiveEffects.FirstOrDefault(e => e.skillType == effect.skillType && e.source == unit);
                if (targetEffect != null)
                {
                    ally.stats.defense -= targetEffect.value;
                    ally.activePassiveEffects.Remove(targetEffect);
                }
            }
        }},

        { UnitSkillType.DoubleAttack, (unit, effect) =>
            {
                var behavior = new DoubleAttackSkill();
                behavior.Remove(unit, effect);
            }
        },
        
        // GrowBuffOverTime 해제
        { UnitSkillType.GrowBuffOverTime, (unit, effect) =>
            {
                new GrowBuffOverTimeSkill().Remove(unit, effect);
            }
        },

        { UnitSkillType.PassiveAreaBuff, (unit, effect) =>
            {
                new PassiveAreaBuffSkill().Execute(unit, null, effect);
            }
        },
        
        // 신규 효과는 여기만 추가 
        };

    //지연 발동 버프관련 / 하이브리드 기병 
    private static IEnumerator DelayedBuffRoutine(UnitCombatFSM unit, SkillEffect effect) 
    {
        yield return new WaitForSeconds(effect.skillDelayTime);
        unit.ApplyBuff(effect.buffStat, effect.skillValue, effect.skillDuration, effect.isPercent);
    }

    // ----- [적용/해제 실행부] -----
    public void ApplyBuffEffects()
    {
        if (skillData == null || skillData.effects == null) return;
        
        foreach (var effect in skillData.effects)
        {
            //패시브만 적용(지속시간 0 이하만)
            if (effect.skillDuration <= 0f)
            {
                if (applyEffectMap.TryGetValue(effect.skillType, out var apply))
                    apply(this, effect); // DelayBuff, IncreaseDefense 등 커스텀 map을 반드시 태움
                else
                    ApplyBuff(effect.buffStat, effect.skillValue, effect.skillDuration, effect.isPercent);
            } 
        }
    }
    
    IEnumerator ApplyPassiveEffectsDelayed() 
    {
        yield return null; // 한 프레임 대기 (필요시 yield return new WaitForSeconds(0.05f); 도 가능) / 유닛 생성 순서/Start 타이밍 이슈를 해결하기 위해
        ApplyBuffEffects();
    }


    public void RemovePassiveEffects()
    {
        if (skillData == null || skillData.effects == null) return;
        foreach (var effect in skillData.effects)
        {
            if (removeEffectMap.TryGetValue(effect.skillType, out var remove))
                remove(this, effect);
        }
    }

    public void ApplyBarrier(float amount, float duration)
    {
        stats.barrier += amount;
        StartCoroutine(RemoveBarrierAfter(duration, amount));
    }

    private IEnumerator RemoveBarrierAfter(float duration, float amount)
    {
        yield return new WaitForSeconds(duration);
        stats.barrier -= amount;
        if (stats.barrier < 0f) stats.barrier = 0f;
    }



    //스탯 버프 관련 딕셔너리 Map ModifyStat
    private static readonly Dictionary<BuffStat, System.Action<RuntimeUnitStats, float, bool, bool>> statModifierMap =
    new Dictionary<BuffStat, System.Action<RuntimeUnitStats, float, bool, bool>>()
    {
        // isPercent = true → 곱셈(1 + v), false → 가산(+=)
        // isRemove = true → 해제(곱셈은 /=, 가산은 -=)
        { BuffStat.Attack,      (s, v, isPer, isRemove) => {
            if (isPer)
            {
                if (isRemove) s.attack /= (1f + v);  // 해제: 나누기
                else s.attack *= (1f + v);           // 적용: 곱하기
            }
            else
            {
                if (isRemove) s.attack -= v;
                else s.attack += v;
            }
        }},
        { BuffStat.Defense,     (s, v, isPer, isRemove) => {
            if (isPer)
            {
                if (isRemove) s.defense /= (1f + v);
                else s.defense *= (1f + v);
            }
            else
            {
                if (isRemove) s.defense -= v;
                else s.defense += v;
            }
        }},
        { BuffStat.MoveSpeed,   (s, v, isPer, isRemove) => {
            if (isPer)
            {
                if (isRemove) s.moveSpeed /= (1f + v);
                else s.moveSpeed *= (1f + v);
            }
            else
            {
                if (isRemove) s.moveSpeed -= v;
                else s.moveSpeed += v;
            }
        }},
        { BuffStat.AttackSpeed, (s, v, isPer, isRemove) => {
            if (isPer)
            {
                if (isRemove) s.attackSpeed /= (1f + v);
                else s.attackSpeed *= (1f + v);
            }
            else
            {
                if (isRemove) s.attackSpeed -= v;
                else s.attackSpeed += v;
            }
        }},
        { BuffStat.Health,      (s, v, isPer, isRemove) => {
            if (isPer)
            {
                if (isRemove) s.health /= (1f + v);
                else s.health *= (1f + v);
            }
            else
            {
                if (isRemove) s.health -= v;
                else s.health += v;
            }
        }},
        { BuffStat.AttackDistance, (s, v, isPer, isRemove) => {
            if (isPer)
            {
                if (isRemove) s.attackDistance /= (1f + v);
                else s.attackDistance *= (1f + v);
            }
            else
            {
                if (isRemove) s.attackDistance -= v;
                else s.attackDistance += v;
            }
        }},
        { BuffStat.DamageReduction, (s, v, isPer, isRemove) => {
            // 피해감소는 누적형(가산)만 사용
            if (isRemove) s.damageReduction -= v;
            else s.damageReduction += v;
        }},
        { BuffStat.CriticalChance, (s, v, isPer, isRemove) => {
            if(isPer)
            {
                if(isRemove) s.criticalChance /= (1f + v);
                else s.criticalChance *= (1f + v);
            }
            else
            {
                if(isRemove) s.criticalChance -= v;
                else s.criticalChance += v;
            }
        }},
        // 필요한 스탯 계속 추가
    };

    public void ModifyStat(BuffStat stat, float value, bool isPercent = false, bool isRemove = false)
    {
        if (statModifierMap.TryGetValue(stat, out var apply))
        {
            apply(stats, value, isPercent, isRemove);
        }
        // 부가처리: 이동속도 등
        if (stat == BuffStat.MoveSpeed)
            agent.speed = stats.moveSpeed;
    }







    //런타임 사거리 
    private LineRenderer rangeIndicator;

    void CreateRangeIndicator()
    {
        GameObject rangeObj = new GameObject("RangeIndicator");
        rangeObj.transform.SetParent(null);

        rangeIndicator = gameObject.AddComponent<LineRenderer>();
        rangeIndicator.positionCount = 51;
        rangeIndicator.loop = true;
        rangeIndicator.widthMultiplier = 0.05f;
        rangeIndicator.material = new Material(Shader.Find("Sprites/Default"));
        rangeIndicator.startColor = Color.red;
        rangeIndicator.endColor = Color.red;
        rangeIndicator.useWorldSpace = true;

        UpdateRangeIndicator();
    }

    void UpdateRangeIndicator()
    {
        if (rangeIndicator == null || unitData == null) return;

        float radius = agent.stoppingDistance;
        Vector3 center = transform.position;

        for (int i = 0; i < 51; i++)
        {
            float angle = i * (360f / 50f) * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0.05f, Mathf.Sin(angle) * radius);
            rangeIndicator.SetPosition(i, center + pos);
        }
    }
}



