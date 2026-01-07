using UnityEngine;
using UnityEngine.AI;
using UnitRoleTypes.Enums;
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
    [HideInInspector] public bool disableBasicAttack = false; //평타 비활성화
    [HideInInspector] public BlindSystem blind;        // 실명 여부
    public float currentHP;
    public float criticalMultiplier = 1.5f;
    public float skillTimer = 0f; // 스킬 쿨다운 누적 
    public SkillData skillData;
    public UnitCombatFSM targetAlly; //힐 버프 대상 
    public System.Action OnPostAttack;
    public bool lastAttackWasCritical; // 직전 평타가 치명타였는지 표시
    public bool movementLocked = false; //오라 유닛 이동 불가용

    public SkillExecutor skillExecutor = new SkillExecutor();
    private UnitState currentState;
    public RuntimeUnitStats stats; // 복사된 인스턴스 스텟 

    public bool isProcessingSkill = false; // 중복 상태 전환 방지용 
    public bool isSilenced = false;


    public List<AppliedPassiveEffect> activePassiveEffects = new List<AppliedPassiveEffect>();

    public Action<float, UnitCombatFSM> OnReflectDamage;

    public delegate void BeforeTakeDamageHandler(ref float damage, UnitCombatFSM attacker); //ref float damage: 실제 적용될 피해값을 수정할 수 있도록 참조 전달
    public event BeforeTakeDamageHandler OnBeforeTakeDamage;

    public event System.Action<float, UnitCombatFSM> OnAfterTakeDamage; // (받은 실제 피해, 가해자)
    public event System.Action<float, UnitCombatFSM> OnDealDamage;      // (내가 입힌 실제 피해, 피해자)
    public event System.Action<UnitCombatFSM> OnKillEnemy;              // (내가 처치한 대상)

    // UI/로그/리플레이 등 외부 시스템이 표준 데이터로 받는 이벤트
    public event Action<HpSnapshot> OnHpChanged;
    public event Action<DamageResult> OnDamageApplied;
    public event Action<HealResult> OnHealed;

    [SerializeField]
    private float attackDistanceWorldScale = 5f; // 기존 로직(stats.attackDistance * 5)을 유지하기 위한 스케일

    private const float minstoppingdistance = 1f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        blind = GetComponent<BlindSystem>();
        if (blind == null) blind = gameObject.AddComponent<BlindSystem>();
    }
    void Start()
    {
        CreateRangeIndicator(); //런타임 사거리

        CloneStats(); // 스탯 복사 
        currentHP = stats.health;
        agent.speed = stats.moveSpeed;
        //agent.stoppingDistance = stats.attackDistance * 5;
        SyncAttackRangeToAgent();

        PublishHpSnapshot();
        ChangeState(new IdleState(this));

        // 패시브 스킬 
        StartCoroutine(ApplyPassiveEffectsDelayed());

    }


    void Update()
    {
        skillTimer += Time.deltaTime; // 스킬 쿨타이머 

        // 스킬 우선 타겟 전환 체크
        if (!isProcessingSkill && skillData != null && skillExecutor.ShouldMoveToSkillTarget(this, skillData))
        {
            isProcessingSkill = true;
            agent.ResetPath();
            //ChangeState(new MoveState(this, true)); // true = 아군 타겟팅
            bool isAllyTarget = (targetAlly != null && targetEnemy == null);
            ChangeState(new MoveState(this, isAllyTarget));
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

    public void OnDeath()
    {
        RemovePassiveEffects(); // 패시브 해제
        OnReflectDamage = null; // 반사 효과도 제거
        OnBeforeTakeDamage = null; // 사전 딜 제거
        OnAfterTakeDamage = null; // 사후 딜 제거
        OnDealDamage = null;
        OnKillEnemy = null;

        if (skillData != null && skillData.effects != null)
        {
            foreach (var effect in skillData.effects)
            {
                var behavior = SkillExecutor.GetSkillBehavior(effect.skillType);
                behavior?.Remove(this, effect);
            }
        }
    }



    public bool TryUseSkill() // 기존 FSM 상태에서 이 메서드만 호출하면 됨
    {
        if (isSilenced)
        {
            Debug.Log("[TryUseSkill] 침묵 상태로 인해 취소됨");
            return false; // 침묵 시 스킬 사용 불가 
        }
        
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
        //이동 불가 상태면 State 전환 자체를 막음
        if (movementLocked && newState is MoveState)
        {
            if (agent != null)
            {
                agent.ResetPath();
                agent.isStopped = true;
                agent.speed = 0f;
            }
            return;
        }
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
        if (IsStunned()) return;
        //평타 비활성화면 공격 로직X
        if (disableBasicAttack) return;

        if (blind != null && blind.IsBlinded)
        {
            Debug.Log($"[Blind] {name} is blinded → Basic attack MISS");
            // TODO: MISS 팝업/사운드가 있다면 여기서 트리거
            return;
        }

        if (targetEnemy == null || !targetEnemy.IsAlive()) return;

        float baseDamage = stats.attack;

        //치명타 판정 
        bool isCritical = UnityEngine.Random.value < stats.criticalChance;
        lastAttackWasCritical = isCritical;

        if (isCritical)
        {
            baseDamage *= criticalMultiplier; // 치명타 배율 적용 
            Debug.Log($"[Critical] {gameObject.name} → 치명타!");
        }

        //payload로 전달
        DamageKind kind = isCritical ? DamageKind.Critical : DamageKind.Normal;
        targetEnemy.TakeDamage(new DamagePayload(baseDamage, this, kind));
        
        //후처리용 이벤트 :추가 타격, 버프, 출혈 등 모든 후처리를 이곳에서 수행 가능 
        OnPostAttack?.Invoke();
    }

    // public void TakeDamage(float damage, UnitCombatFSM attacker = null)
    // {
    //     if (stats.guardCount > 0)
    //     {
    //         stats.guardCount--;
    //         Debug.Log($"[가드] {gameObject.name} -> 피격 방어 남은 가드: {stats.guardCount}");
    //         return;
    //     }

    //     //Clamp 처리: damageReduction이 1.0 이상이면 최소 0, 음수면 최대 1
    //     float reductionFactor = Mathf.Clamp01(1.0f - stats.damageReduction);
    //     float effectiveDamage = damage * (100f / (100f + stats.defense));
    //     effectiveDamage *= reductionFactor;

    //     //받는 피해 수정 훅 (표식 등)
    //     OnBeforeTakeDamage?.Invoke(ref effectiveDamage, attacker);


    //     // 방어막 우선 차감 
    //     if (stats.barrier > 0f)
    //     {
    //         float shieldAbsorb = Math.Min(stats.barrier, effectiveDamage);
    //         stats.barrier -= shieldAbsorb;
    //         effectiveDamage -= shieldAbsorb;
    //         Debug.Log($"[방어막 차감] {gameObject.name}: {shieldAbsorb}만큼 흡수, 남은 방어막: {stats.barrier}");
    //     }
    //     if (effectiveDamage > 0)
    //     {
    //         currentHP -= effectiveDamage;
    //     }
        
    //     Debug.Log($"[피격] {name} - 받은 데미지: {effectiveDamage:F1} / 남은 HP: {currentHP:F1}");

    //     //가해자에게 실제 입힌 피해 알림 (피흡 같은 패시브는 이걸 사용) 
    //     if (attacker != null && effectiveDamage > 0f)
    //     {
    //         attacker.OnDealDamage?.Invoke(effectiveDamage, this);
    //     }

    //     bool isDeadNow = currentHP <= 0f;

    //     //가해자에게 처치 알림
    //     if (attacker != null && isDeadNow)
    //     {
    //         attacker.OnKillEnemy?.Invoke(this);
    //     }

    //     if (currentHP <= 0)
    //     {
    //         ChangeState(new DeadState(this));
    //     }
        
    //     // 피해 이후 : HP에 실제 반영된 피해량 기준(>0일 때만)
    //     if(effectiveDamage > 0)
    //         OnAfterTakeDamage?.Invoke(effectiveDamage, attacker);
    
    //     // 데미지 반사 처리
    //     if (attacker != null && OnReflectDamage != null)
    //     {
    //         Debug.Log($"[Reflect] 반사 발동 - {this.name} ← {attacker.name}");
    //         OnReflectDamage.Invoke(effectiveDamage, attacker);
    //     }
    // }

public void TakeDamage(float damage, UnitCombatFSM attacker = null)
{
    TakeDamage(DamagePayload.FromLegacy(damage, attacker));
}

// 신규 표준 진입점(앞으로 치명타/도트 구분은 여기 Kind로 들어오게 됨)
public void TakeDamage(DamagePayload payload)
{
    ApplyDamageInternal(payload);
}

private void ApplyDamageInternal(DamagePayload payload)
{
    if (stats.guardCount > 0)
    {
        stats.guardCount--;
        Debug.Log($"[가드] {gameObject.name} -> 피격 방어 남은 가드: {stats.guardCount}");
        return;
    }

    float rawDamage = payload.RawDamage;
    UnitCombatFSM attacker = payload.Attacker;

    // 1) 방어/피해감소 적용
    float reductionFactor = Mathf.Clamp01(1.0f - stats.damageReduction);
    float mitigatedDamage = rawDamage * (100f / (100f + stats.defense));
    mitigatedDamage *= reductionFactor;

    // 2) 받는 피해 수정 훅(표식 등)
    OnBeforeTakeDamage?.Invoke(ref mitigatedDamage, attacker);

    // 3) 방어막 우선 차감
    float barrierAbsorbed = 0f;
    float hpDamage = mitigatedDamage;

    if (stats.barrier > 0f && hpDamage > 0f)
    {
        float shieldAbsorb = Math.Min(stats.barrier, hpDamage);
        stats.barrier -= shieldAbsorb;
        barrierAbsorbed = shieldAbsorb;
        hpDamage -= shieldAbsorb;

        Debug.Log($"[방어막 차감] {gameObject.name}: {shieldAbsorb}만큼 흡수, 남은 방어막: {stats.barrier}");
    }

    // 4) HP 차감
    if (hpDamage > 0f)
    {
        currentHP -= hpDamage;
    }

    Debug.Log($"[피격] {name} - 받은 데미지: {hpDamage:F1} / 남은 HP: {currentHP:F1}");

    // HP 또는 방어막이 변했으면 체력바 갱신 이벤트
    if (hpDamage > 0f || barrierAbsorbed > 0f)
    {
        NotifyHpChanged();
    }

    // 5) 가해자에게 실제 입힌 피해 알림(피흡 등)
    if (attacker != null && hpDamage > 0f)
    {
        attacker.OnDealDamage?.Invoke(hpDamage, this);
    }

    bool isDeadNow = currentHP <= 0f;

    // 6) 처치 이벤트
    if (attacker != null && isDeadNow)
    {
        attacker.OnKillEnemy?.Invoke(this);
    }

    if (isDeadNow)
    {
        ChangeState(new DeadState(this));
    }

    // 7) 기존 사후 피격 이벤트(HP 반영된 피해 기준)
    if (hpDamage > 0f)
    {
        OnAfterTakeDamage?.Invoke(hpDamage, attacker);
    }

    // 8) 신규 표준 결과 이벤트(UI는 이걸 주로 쓰게 됨)
    if (mitigatedDamage > 0f)
    {
        var result = new DamageResult(
            target: this,
            attacker: attacker,
            kind: payload.Kind,
            rawDamage: rawDamage,
            mitigatedDamage: mitigatedDamage,
            barrierAbsorbed: barrierAbsorbed,
            hpDamage: hpDamage,
            isKilled: isDeadNow
        );

        OnDamageApplied?.Invoke(result);
    }

    // 9) 데미지 반사 처리(기존 로직 유지)
    if (attacker != null && OnReflectDamage != null)
    {
        Debug.Log($"[Reflect] 반사 발동 - {this.name} ← {attacker.name}");
        OnReflectDamage.Invoke(hpDamage, attacker);
    }
}


    // ConeTripleHit 헬퍼 List
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
            attack = unitData.attackValue,
            defense = unitData.defense,
            attackSpeed = unitData.attackSpeed,
            attackDistance = unitData.attackDistance,
            criticalChance = unitData.roleType switch
            {
                RoleTypes.Melee => 0.1f,
                RoleTypes.Ranged => 0.3f,
                RoleTypes.Support => 0.05f,
                _ => 0.1f
            }
        };

    }


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

    // public void ReceiveHealing(float amount)
    // {
    //     currentHP += amount;

    //     if (currentHP > stats.health) currentHP = stats.health;
    //     Debug.Log($"[회복] {gameObject.name} → {amount} 회복 / 현재 HP: {currentHP:F1}");
    // }

    public void ReceiveHealing(float amount)
    {
        ReceiveHealing(HealPayload.FromLegacy(amount));
    }

    public void ReceiveHealing(HealPayload payload)
    {
        if (stats == null) return;

        float before = currentHP;

        currentHP += payload.RawAmount;
        if (currentHP > stats.health) currentHP = stats.health;

        float applied = currentHP - before;
        float overheal = payload.RawAmount - applied;
        if (overheal < 0f) overheal = 0f;

        Debug.Log($"[회복] {gameObject.name} → {payload.RawAmount} 회복 / 현재 HP: {currentHP:F1}");

        if (applied > 0f)
        {
            NotifyHpChanged();
        }

        var result = new HealResult(
            target: this,
            healer: payload.Healer,
            kind: payload.Kind,
            rawAmount: payload.RawAmount,
            appliedAmount: applied,
            overheal: overheal
        );

        OnHealed?.Invoke(result);
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
 
    //스킬 쿨타임 조건 확인 
    public bool CanUseSkill()
    {
        if (IsStunned()) return false;
        return skillData != null && skillTimer >= skillData.skillCoolDown;
    }


    private static readonly Dictionary<UnitCombatFSM, BeforeTakeDamageHandler> _splitHooks
        = new Dictionary<UnitCombatFSM, BeforeTakeDamageHandler>(); 

    // ----- [조건부 버프 및 스킬(패시브형 관련) : 효과 적용/해제 함수맵] -----
 
    private static readonly Dictionary<UnitSkillType, System.Action<UnitCombatFSM, SkillEffect>> applyEffectMap =
        new Dictionary<UnitSkillType, System.Action<UnitCombatFSM, SkillEffect>>()
    {
        // 근접형 아군 전체 방어력 5% 증가 (자기 자신 포함, 모든 근접 아군)/ 기어 방패병
        { UnitSkillType.IncreaseDefense, (unit, effect) => {
            foreach (var ally in GameObject.FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None))
            {
                if (!ally.IsAlive()) continue;
                if (ally.unitData.faction != unit.unitData.faction) continue;
                if (ally.unitData.roleType != RoleTypes.Melee) continue;
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
        { UnitSkillType.PassiveRegenAndSplitDamage, (unit, effect) =>
                {
                    // 재생 시작
                    var regen = unit.GetComponent<RegenStatus>();
                    if (regen == null) regen = unit.gameObject.AddComponent<RegenStatus>();

                    float interval   = (effect.skillDelayTime > 0f) ? effect.skillDelayTime : 2f;  // 기본 2초
                    float amount     = (effect.skillValue     > 0f) ? effect.skillValue     : 0.05f;// 기본 5%
                    bool  isPercent  = (effect.isPercent) ? true : true; // 기본 퍼센트 사용
                    float duration   = 0f; // 패시브 무한

                    regen.StartPulse(interval, amount, isPercent, duration);

                    // 70/30 피해 분할 
                    if (!_splitHooks.ContainsKey(unit))
                    {
                        var selfDot = unit.GetComponent<DeferredSelfDamageStatus>();
                        if (selfDot == null) selfDot = unit.gameObject.AddComponent<DeferredSelfDamageStatus>();

                        float ratio30   = (effect.skillMaxStack > 0f) ? effect.skillMaxStack : 0.30f; // 30%
                        float dotDur    = (effect.skillRange     > 0f) ? effect.skillRange    : 3.0f; // 3초
                        int   dotTicks  = 3; // 3틱 고정(원하면 별도 필드로 뺄 수 있음)

                        BeforeTakeDamageHandler h = (ref float effDmg, UnitCombatFSM attacker) =>
                        {
                            // 내부 지연피해(attacker == null)는 다시 분할하면 무한루프 → 스킵
                            if (attacker == null) return;

                            float deferred = effDmg * ratio30; // 30%를 지연
                            effDmg -= deferred;                // 즉시반영은 70%만

                            // 남은 30%는 3초 동안 3틱으로 자기 자신에게 '추가 피해'
                            selfDot.AddSplitDamage(deferred, dotTicks, dotDur);
                        };

                        unit.OnBeforeTakeDamage += h;  // 피해계산(방어/감뎀 후, 방어막 전) 단계에서 개입
                        _splitHooks[unit] = h;
                    }
                }    
            },
            { UnitSkillType.BleedOnCritPassive, (unit, effect) =>
                {
                    new BleedOnCritPassiveSkill().Execute(unit, null, effect);
                }
            },
            { UnitSkillType.LifeStealAndKillHealPassive, (unit, effect) =>
                {
                    new LifeStealAndKillHealPassiveSkill().Execute(unit, null, effect);
                }
            },
            { UnitSkillType.StackingHasteThenExhaustPassive, (unit, effect) =>
                {
                    new StackingHasteThenExhaustPassiveSkill().Execute(unit, null, effect);
                }
            },
            { UnitSkillType.ImmobileAuraBuff, (unit, effect) =>
                {
                    new ImmobileAuraBuffSkill().Execute(unit, null, effect);
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
                if (ally.unitData.roleType != RoleTypes.Melee) continue;
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

        { UnitSkillType.PassiveRegenAndSplitDamage, (unit, effect) =>
            {
                var regen = unit.GetComponent<RegenStatus>();
                if (regen != null) regen.ClearAll();

                var selfDot = unit.GetComponent<DeferredSelfDamageStatus>();
                if (selfDot != null) selfDot.ClearAll();

                if (_splitHooks.TryGetValue(unit, out var h))
                {
                    unit.OnBeforeTakeDamage -= h; // 구독 해제
                    _splitHooks.Remove(unit);
                }
            }
        },
        { UnitSkillType.BleedOnCritPassive, (unit, effect) =>
            {
                new BleedOnCritPassiveSkill().Remove(unit, effect);
            }
        },
        { UnitSkillType.LifeStealAndKillHealPassive, (unit, effect) =>
            {
                new LifeStealAndKillHealPassiveSkill().Remove(unit, effect);
            }
        },
        { UnitSkillType.StackingHasteThenExhaustPassive, (unit, effect) =>
            {
                new StackingHasteThenExhaustPassiveSkill().Remove(unit, effect);
            }
        },
        { UnitSkillType.ImmobileAuraBuff, (unit, effect) =>
            {
                new ImmobileAuraBuffSkill().Remove(unit, effect);
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

        // 부가처리: 사거리
        if (stat == BuffStat.AttackDistance)
        {
            SyncAttackRangeToAgent();
        }
    }

    //특정 범위 내 적 유닛 탐색 리스트 반환 
    public List<UnitCombatFSM> FindEnemiesInRange(float range)
    {
        var results = new List<UnitCombatFSM>();

        foreach (var unit in FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None))
        {
            if (unit == this || !unit.IsAlive()) continue;
            if (unit.unitData.faction == this.unitData.faction) continue; // 같은 진영 제외

            float distance = Vector3.Distance(transform.position, unit.transform.position);
            if (distance <= range)
            {
                results.Add(unit);
            }
        }

        return results;
    }

public static class UnitCombatFSM_DebuffRegistry
{
    public class DebuffRec
    {
        public UnitCombatFSM target;
        public BuffStat stat;
        public float appliedAmount;   // 실제 적용한 값(감소는 음수)
        public bool isPercent;
        public Coroutine routine;
    }

    private static readonly Dictionary<UnitCombatFSM, List<DebuffRec>> _map = new();

    /// <summary>
    /// 추적형 스탯 디버프 적용
    /// - amount: 비율(0.15=15%) 또는 고정 수치 (isPercent로 구분)
    /// - 감소는 음수로 적용, 해제 시 같은 값을 isRemove=true로 되돌림
    /// </summary>
    public static void ApplyStatDebuffTracked(UnitCombatFSM target, BuffStat stat, float amount, float duration, bool isPercent)
    {
        if (target == null || !target.IsAlive() || amount <= 0f || duration <= 0f) return;

        // 감소 디버프이므로 '음수'로 변환
        float applied = -Mathf.Abs(amount);

        var rec = new DebuffRec
        {
            target = target,
            stat = stat,
            appliedAmount = applied,
            isPercent = isPercent
        };

        rec.routine = target.StartCoroutine(CoApply(target, rec, duration));

        if (!_map.TryGetValue(target, out var list))
        {
            list = new List<DebuffRec>();
            _map[target] = list;
        }
        list.Add(rec);
    }

    private static IEnumerator CoApply(UnitCombatFSM t, DebuffRec r, float duration)
    {
        // 디버프 적용(감소: 음수, 증가 아님)
        t.ModifyStat(r.stat, r.appliedAmount, r.isPercent, isRemove: false);

        yield return new WaitForSeconds(duration);

        // 디버프 해제(같은 값 + isRemove=true → 자연 복구)
        t.ModifyStat(r.stat, r.appliedAmount, r.isPercent, isRemove: true);

        if (_map.TryGetValue(t, out var list)) list.Remove(r);
    }

    /// <summary>정화: 진행 중인 추적형 스탯 디버프를 전부 해제</summary>
    public static void CleanseAllStatDebuffs(UnitCombatFSM target)
    {
        if (target == null) return;
        if (!_map.TryGetValue(target, out var list) || list.Count == 0) return;

        foreach (var rec in list)
        {
            if (rec.routine != null) target.StopCoroutine(rec.routine);
            // 같은 값 + isRemove=true 로 복구
            target.ModifyStat(rec.stat, rec.appliedAmount, rec.isPercent, isRemove: true);
        }
        list.Clear();
    }
}

public bool IsStunned()
{
    return TryGetComponent<StunSystem>(out var s) && s.IsStunned;
}



public HpSnapshot GetHpSnapshot()
{
    if (stats == null)
        return new HpSnapshot(currentHP, 0f, 0f);

    return new HpSnapshot(currentHP, stats.health, stats.barrier);
}

// UI 바인더가 구독 직후 한 번 호출해서 초기값 동기화할 수 있게 제공
public void PublishHpSnapshot()
{
    OnHpChanged?.Invoke(GetHpSnapshot());
}

private void NotifyHpChanged()
{
    // stats가 아직 준비되지 않은 타이밍(예: Awake/초기화 전) 보호
    if (stats == null) return;

    OnHpChanged?.Invoke(new HpSnapshot(currentHP, stats.health, stats.barrier));
}


// stats.attackDistance(스탯 단위)를 NavMeshAgent가 쓰는 월드 단위로 변환한 값
public float GetAttackRangeWorld()
{
    if (stats == null) return agent != null ? agent.stoppingDistance : 0f;
    return Mathf.Max(minstoppingdistance, stats.attackDistance * attackDistanceWorldScale);
}

// stats.attackDistance 변경을 실제 교전 거리(agent.stoppingDistance)에 반영
private void SyncAttackRangeToAgent()
{
    if (agent == null) return;

    float desired = GetAttackRangeWorld();
    agent.stoppingDistance = desired;
}










    //---------------------------------------------------------------------------------------------------------------------
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



