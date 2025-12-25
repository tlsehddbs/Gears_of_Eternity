using UnityEngine;
using UnityEngine.AI;
using UnitRoleTypes.Enums;


public class UnitCombat : MonoBehaviour
{
    public UnitCardData unitData; // ScriptableObject 연결 
    private UnitCombat targetEnemy; // 현재 공격중인 적 
    private NavMeshAgent agent;
    private float currentHP;
    private float attackTimer;
    private float lookAttackDistance;
    private float criticalChance;
    public float criticalMultiplier = 1.5f; //치명타 배수 
   
   
    private void Awake()
    {
        CreateRangeIndicator(); //런타임 사거리
    }

    void Start()
    {
        lookAttackDistance = unitData.attackDistance * 5;

        currentHP = unitData.health;
        agent = GetComponent<NavMeshAgent>();

        if(agent != null)
        {
            agent.speed = unitData.moveSpeed;
            agent.stoppingDistance = unitData.attackDistance * 5;
        }

        switch(unitData.roleType)
        {
            case RoleTypes.Melee:
                criticalChance =0.1f;
                break;
            case RoleTypes.Ranged:
                criticalChance = 0.3f;
                break;
            case RoleTypes.Support:
                criticalChance = 0.1f;
                break;
        }

    }

    // Update is called once per frame
    void Update()
    {
        attackTimer += Time.deltaTime;

        //런타임 사거리표시 
        if (rangeIndicator != null)
        {
            UpdateRangeIndicator(); 
        }

        if(targetEnemy != null && targetEnemy.IsAlive())
        {
            float distance = Vector3.Distance(transform.position, targetEnemy.transform.position);

            if(distance > agent.stoppingDistance + 0.05f)
            {
                // 공격 사거리 밖이면 이동 
                if(agent != null)
                {
                    agent.SetDestination(targetEnemy.transform.position);
                    //Debug.Log($"[이동 중] Target = {targetEnemy.name}, Distance = {distance}");
                }
            }
            else
            {
                // 공격사거리 안이면 이동 멈추고 공격 
                if(agent != null && agent.hasPath)
                {
                    agent.ResetPath();
                    Debug.Log("[공격 범위 진입] 이동 중단");
                }

                if(attackTimer >= unitData.attackSpeed)
                {
                    Attack();
                    attackTimer = 0f;
                }  
            }
        }
        else
        {
            FindNewTarget();
        }
    }

    public bool IsAlive()
    {
        return currentHP > 0;
    }

    //공격
    void Attack()
    {
        if(targetEnemy != null)
        {
            float baseDamage = unitData.attack;

            //치명타 여부 결정 
            bool isCritical = UnityEngine.Random.value < criticalChance;
            if(isCritical)
            {
                baseDamage *= criticalMultiplier;
                Debug.Log("[Critical]");
                
            }

            targetEnemy.TakeDamage(baseDamage);
            Debug.Log($"[공격] {targetEnemy.name}에게 {baseDamage} 데미지 / 입힌 데미지{(baseDamage * (100f / (100f + targetEnemy.unitData.defense))):F1}.");
        }
    }

    //데미지 입는 방식
    public void TakeDamage(float damage)
    {
        float effectiveDamage = damage * (100f / (100f + unitData.defense));
        currentHP -= effectiveDamage;

        Debug.Log($"[피격] {gameObject.name} - 받은 데미지: {effectiveDamage:F1} / 남은 HP: {currentHP:F1}");
        if(currentHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }

    //타겟 찾는 로직 
    void FindNewTarget()
    {
        float shortestDistance = Mathf.Infinity;
        UnitCombat nearestEnemy = null;

        UnitCombat[] units = FindObjectsOfType<UnitCombat>();
        if(units == null || units.Length == 0)
        {
            targetEnemy = null;
            return;
        }

        foreach(var unit in units)
        {
            if(unit == this || !unit.IsAlive()) continue;

            if(unit.unitData.faction == this.unitData.faction) continue;


            float dist = Vector3.Distance(transform.position, unit.transform.position);
            if(dist < shortestDistance)
            {
                shortestDistance = dist;
                nearestEnemy = unit;
            }
        }

        targetEnemy = nearestEnemy;
    }

    void OnDrawGizmosSelected()
    {
        if (unitData == null) return;

        Gizmos.color = Color.red; 
        Gizmos.DrawWireSphere(transform.position, lookAttackDistance);
    }



    //런타인 사거리 표시
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

        float radius = lookAttackDistance;
        Vector3 center = transform.position;

        for(int i = 0; i < 51; i++)
        {
            float angle = i * (360f / 50f) * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0.05f, Mathf.Sin(angle) * radius);
            rangeIndicator.SetPosition(i, center + pos);
        }
    }
}