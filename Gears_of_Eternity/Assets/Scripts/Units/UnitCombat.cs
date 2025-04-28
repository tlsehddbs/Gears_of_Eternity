using System;
using Unity.Android.Gradle.Manifest;
using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class UnitCombat : MonoBehaviour
{
    public UnitCardData unitData; // ScriptableObject 연결 
    private float currentHP;
    private float attackTimer;

    private UnitCombat targetEnemy; // 현재 공격중인 적 
    private NavMeshAgent agent;

    

    void Start()
    {
        currentHP = unitData.health;
        agent = GetComponent<NavMeshAgent>();

        if(agent != null)
        {
            agent.speed = unitData.moveSpeed;
            agent.stoppingDistance = unitData.attackRange;
        }
        
        CreateRangeIndicator(); //런타임 사거리리

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

    void Attack()
    {
        if(targetEnemy != null)
        {
            targetEnemy.TakeDamage(unitData.attack);
            Debug.Log($"[공격] {targetEnemy.name}에게 {unitData.attack - targetEnemy.unitData.defense} 데미지를 입혔습니다.");
        }
    }

    public void TakeDamage(float damage)
    {
        float effectiveDamage = Mathf.Max(damage - unitData.defense, 1f);
        currentHP -= effectiveDamage;

        if(currentHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }

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
        Gizmos.DrawWireSphere(transform.position, unitData.attackRange);
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

        float radius = unitData.attackRange;
        Vector3 center = transform.position;

        for(int i = 0; i < 51; i++)
        {
            float angle = i * (360f / 50f) * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0.05f, Mathf.Sin(angle) * radius);
            rangeIndicator.SetPosition(i, center + pos);
        }
    }
}