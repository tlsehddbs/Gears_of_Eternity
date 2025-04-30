using UnityEngine;
using UnityEngine.AI;
using BattleTypes.Enums;

public class UnitCombatFSM : MonoBehaviour
{
    public UnitCardData unitData; // 원본 ScriptableObjcet
    public RuntimeUnitStats  stats; // 복사된 인스턴스 스텟 
    public NavMeshAgent agent;
    public UnitCombatFSM targetEnemy; //현재 타겟 Enemy 

    [HideInInspector] public float attackTimer;
    private float currentHP;
    private float criticalChance;
    public float criticalMultiplier = 1.5f;
    public float skillTimer; // 스킬 쿨다운 누적 
    public UnitCombatFSM targetAlly; //힐 버프 대상 
    public bool CanUseSkill() => skillTimer >= unitData.skillCoolDown;

    private UnitState currentState;

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
    }

    void Update()
    {
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
        Debug.Log($"[공격] {targetEnemy.name}에게 {baseDamage} 데미지 / 입힌 데미지{(baseDamage * (100f / (100f + targetEnemy.unitData.defense))):F1}.");
    }

    public void TakeDamage(float damage)
    {
        float effectiveDamage = damage * (100f / (100f + stats.defense));
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
        if(rangeIndicator == null || unitData == null) return;

        float radius = agent.stoppingDistance;
        Vector3 center = transform.position;

        for(int i = 0; i < 51; i++)
        {
            float angle = i * (360f / 50f) * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0.05f, Mathf.Sin(angle) * radius);
            rangeIndicator.SetPosition(i, center + pos);
        }
    }
}