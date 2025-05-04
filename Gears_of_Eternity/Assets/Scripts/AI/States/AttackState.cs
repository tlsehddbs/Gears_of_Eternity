using UnityEngine;
using UnitSkillTypes.Enums;
public class AttackState : UnitState
{
    public AttackState(UnitCombatFSM unit) : base(unit){}

    public override void Update()
    {
        unit.attackTimer += Time.deltaTime;

        if(unit.targetEnemy == null || !unit.targetEnemy.IsAlive())
        {
            unit.ChangeState(new IdleState(unit));
            return;
        }

        float dist = Vector3.Distance(unit.transform.position, unit.targetEnemy.transform.position);
        if(dist > unit.agent.stoppingDistance + 0.05f)
        {
            unit.ChangeState(new MoveState(unit, false));
            return;
        }

        if(unit.attackTimer >= unit.stats.attackSpeed)
        {
            unit.Attack();
            unit.attackTimer = 0f;
        }
    }  
    
}
