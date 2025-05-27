using UnityEngine;
using UnityEngine.AI;
using BattleTypes.Enums;
using UnitSkillTypes.Enums;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AppliedPassiveEffect
{
    public UnitSkillType skillType;
    public float value;
    public UnitCombatFSM source;
    
}
public enum BuffStat
{
    Attack,
    Defense,
    MoveSpeed,
    AttackSpeed
}

public partial class UnitCombatFSM : MonoBehaviour
{
    public UnitCardData unitData; // 원본 ScriptableObjcet
    public NavMeshAgent agent;
    public UnitCombatFSM targetEnemy; //현재 타겟 Enemy 
    [HideInInspector] public float attackTimer;
    public float currentHP;
    private float criticalChance;
    public float criticalMultiplier = 1.5f;
    public float skillTimer = 0f; // 스킬 쿨다운 누적 
    public SkillData skillData;
    public UnitCombatFSM targetAlly; //힐 버프 대상 


    private SkillExecutor skillExecutor = new SkillExecutor();
    private UnitState currentState;
    public RuntimeUnitStats stats; // 복사된 인스턴스 스텟 

    public bool isProcessingSkill = false; // 중복 상태 전환 방지용 


    public List<AppliedPassiveEffect> activePassiveEffects = new List<AppliedPassiveEffect>();
    public float damageReductionBonus = 0f;



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

        AssignCriticalChance();
        ChangeState(new IdleState(this));

        // 패시브
        StartCoroutine(ApplyPassiveEffectsDelayed());

    }

    public void OnDeath()
    {
        RemovePassiveEffects(); // 패시브 해제
    }

    void Update()
    {
        skillTimer += Time.deltaTime; // 스킬 쿨타이머 

        // 스킬 사용 우선 FSM 통합 체크
        if (!isProcessingSkill && ShouldUseSkill())
        {
            isProcessingSkill = true;
            agent.ResetPath();
            ChangeState(new MoveState(this, true)); // 서포트 이동 우선
            return;
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
        if (targetEnemy == null) return;
        float baseDamage = stats.attack;
        bool isCritical = UnityEngine.Random.value < criticalChance;

        if (isCritical)
        {
            baseDamage *= criticalMultiplier;
            Debug.Log("[Critical]");
        }

        targetEnemy.TakeDamage(baseDamage);
        
    }

    public void TakeDamage(float damage)
    {
        float reductionFactor = 1.0f - damageReductionBonus;
        float effectiveDamage = damage * (100f / (100f + stats.defense));
        effectiveDamage *= reductionFactor;
        currentHP -= effectiveDamage;
        Debug.Log($"[피격] {name} - 받은 데미지: {effectiveDamage:F1} / 남은 HP: {currentHP:F1}");

        if (currentHP <= 0)
        {
            ChangeState(new DeadState(this));
        }
    }



    public void FindNewTarget()
    {
        float shortestDistance = Mathf.Infinity;
        UnitCombatFSM nearestEnemy = null;

        var all = FindObjectsOfType<UnitCombatFSM>();
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
            attackDistance = unitData.attackDistance
        };

    }

    private void AssignCriticalChance()
    {
        switch (unitData.battleType)
        {
            case BattleType.Melee:
                criticalChance = 0.1f;
                break;
            case BattleType.Ranged:
                criticalChance = 0.3f;
                break;
            case BattleType.Support:
                criticalChance = 0.05f;
                break;
            default:
                criticalChance = 0.1f;
                break;
        }
    }


    public UnitCombatFSM FindNearestEnemy()
    {
        float min = float.MaxValue;
        UnitCombatFSM result = null;

        foreach (var unit in FindObjectsOfType<UnitCombatFSM>())
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

        foreach (var unit in FindObjectsOfType<UnitCombatFSM>())
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

        foreach (var unit in FindObjectsOfType<UnitCombatFSM>())
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

    public void ApplyBuff(BuffStat stat, float amount, float duration)
    {
        StartCoroutine(BuffRoutine(stat, amount, duration));
    }

    public void ApplyDebuff(BuffStat stat, float amount, float duration)
    {
        StartCoroutine(DebuffRoutine(stat, amount, duration));
    }

    //버프 
    private IEnumerator BuffRoutine(BuffStat stat, float amount, float duration)
    {
        ModifyStat(stat, amount); // 스탯 증가 
        yield return new WaitForSeconds(duration);
        ModifyStat(stat, -amount); //스탯 복구 
    }

    //디버프 
    private IEnumerator DebuffRoutine(BuffStat stat, float amount, float duration)
    {
        ModifyStat(stat, -amount); // 스탯 감소
        yield return new WaitForSeconds(duration);
        ModifyStat(stat, amount); // 스탯 복구
    }

    private void ModifyStat(BuffStat stat, float value)
    {
        switch (stat)
        {
            case BuffStat.Attack:
                stats.attack += value;
                break;
            case BuffStat.Defense:
                stats.defense += value;
                break;
            case BuffStat.MoveSpeed:
                stats.moveSpeed += value;
                agent.speed = stats.moveSpeed; //NavMeshAgent에도 적용 
                break;
            case BuffStat.AttackSpeed:
                stats.attackSpeed += value;
                break;
        }
    }


    //스킬 
    public void TryUseSkill()
    {
        if (!CanUseSkill() || skillData == null || skillData.effects == null) return;

        foreach (var effect in skillData.effects)
        {
            UnitCombatFSM skillTarget = null;
            switch (effect.skillType)
            {
                case UnitSkillType.InstantHeal:
                    skillTarget = FindLowestHpAlly();
                    break;
                case UnitSkillType.IncreaseAttack:
                    skillTarget = FindNearestAlly();
                    break;
                case UnitSkillType.AttackDown:
                    skillTarget = FindNearestEnemy();
                    break;
                    // 기타 스킬타입 분기 추가
            }
            skillExecutor.ExecuteSkill(skillData, this, skillTarget);
        }
        skillTimer = 0f;
    }
    public bool CanUseSkill()
    {
        return skillData != null && skillTimer >= skillData.skillCoolDown;
    }

    public bool ShouldUseSkill()
    {
        if (!CanUseSkill() || skillData == null || skillData.effects == null || skillData.effects.Count == 0)
            return false;

        // 단일 효과만 있다고 가정
        var effect = skillData.effects[0];

        switch (effect.skillType)
        {
            case UnitSkillType.InstantHeal:
                targetAlly = FindLowestHpAlly();
                return targetAlly != null && targetAlly.currentHP < targetAlly.stats.health;
            case UnitSkillType.IncreaseAttack:
                targetAlly = FindNearestAlly();
                return targetAlly != null;
            case UnitSkillType.AttackDown:
                targetEnemy = FindNearestEnemy();
                return targetEnemy != null;
            default:
                return false;
        }
    }


    // ----- [패시브: 효과 적용/해제 함수맵] -----
 
    private static readonly Dictionary<UnitSkillType, System.Action<UnitCombatFSM, SkillEffect>> applyEffectMap =
        new Dictionary<UnitSkillType, System.Action<UnitCombatFSM, SkillEffect>>()
    {
        // 자신이 받는 피해 감소 /기어 방패병
        { UnitSkillType.DamageReduction, (unit, effect) => {
            unit.damageReductionBonus += effect.skillValue;
            unit.activePassiveEffects.Add(new AppliedPassiveEffect { skillType = effect.skillType, value = effect.skillValue, source = unit });
        }},

         // 근접형 아군 전체 방어력 5% 증가 (자기 자신 포함, 모든 근접 아군)/ 기어 방패병
        { UnitSkillType.IncreaseDefense, (unit, effect) => {
            foreach (var ally in GameObject.FindObjectsOfType<UnitCombatFSM>())
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
        // 신규 효과는 여기만 추가
    };

    private static readonly Dictionary<UnitSkillType, System.Action<UnitCombatFSM, SkillEffect>> removeEffectMap =
        new Dictionary<UnitSkillType, System.Action<UnitCombatFSM, SkillEffect>>()
    {
        // 자신이 받는 피해 감소 해제 /기어 방패병
        { UnitSkillType.DamageReduction, (unit, effect) => {
            var targetEffect = unit.activePassiveEffects.FirstOrDefault(e => e.skillType == effect.skillType && e.source == unit);
            if (targetEffect != null)
            {
                unit.damageReductionBonus -= targetEffect.value;
                unit.activePassiveEffects.Remove(targetEffect);
            }
        }},
        // 근접형 아군 전체 방어력 5% 증가 해제 (자기 자신 포함, 모든 근접 아군)/ 기어 방패병
        { UnitSkillType.IncreaseDefense, (unit, effect) => {
            foreach (var ally in GameObject.FindObjectsOfType<UnitCombatFSM>())
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
        // 신규 효과는 여기만 추가
    };

    // ----- [적용/해제 실행부] -----
    public void ApplyPassiveEffects()
    {
        if (skillData == null || skillData.effects == null) return;
        foreach (var effect in skillData.effects)
        {
            if (applyEffectMap.TryGetValue(effect.skillType, out var apply))
                apply(this, effect);
        }
    }

    IEnumerator ApplyPassiveEffectsDelayed()
    {
        yield return null; // 한 프레임 대기 (필요시 yield return new WaitForSeconds(0.05f); 도 가능) / 유닛 생성 순서/Start 타이밍 이슈를 해결하기 위해
        ApplyPassiveEffects();
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



