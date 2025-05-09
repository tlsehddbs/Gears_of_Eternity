using UnityEngine;

public class MoveState : UnitState
{
    private bool isSupportTarget;

    public MoveState(UnitCombatFSM unit, bool isAllyTarget) : base(unit)
    {
        isSupportTarget = isAllyTarget;
    }

    public override void Update()
    {
        //적이 사라지거나 죽었을때 상태 변경 
        UnitCombatFSM target = GetCurrentTarget();
        if (target == null || !target.IsAlive())
        {
            unit.ChangeState(new IdleState(unit));
            return;
        }

        float distance = Vector3.Distance(unit.transform.position, target.transform.position);
        float stopping = unit.agent.stoppingDistance;

        if (distance > stopping + 0.05f)
        {
            unit.agent.SetDestination(target.transform.position);
        }
        else
        {
            unit.agent.ResetPath();

            if (isSupportTarget)
            {
                unit.ChangeState(new SupportSkillState(unit));
            }
            else
            {
                unit.ChangeState(new AttackState(unit));
            }
        }
    }

    private UnitCombatFSM GetCurrentTarget()
    {
        return isSupportTarget ? unit.targetAlly : unit.targetEnemy;
    }
}