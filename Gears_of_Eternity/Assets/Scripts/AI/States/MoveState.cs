using UnityEngine;

public class MoveState : UnitState
{
    public MoveState(UnitCombatFSM unit) : base(unit){}

    public override void Update()
    {
        if(unit.targetEnemy == null || !unit.targetEnemy.IsAlive())
        {
            unit.ChangeState(new IdleState(unit));
            return;
        }

        float dist = Vector3.Distance(unit.transform.position, unit.targetEnemy.transform.position);

        if(dist > unit.agent.stoppingDistance + 0.05f)
        {
            unit.agent.SetDestination(unit.targetEnemy.transform.position);
        }
        else
        {
            unit.agent.ResetPath();
            Debug.Log("[공격 범위 진입] 이동 중단");
            unit.ChangeState(new AttackState(unit));
        }
    }
}