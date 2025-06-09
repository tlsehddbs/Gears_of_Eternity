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
        //현재 타켓 결정 (지원/적군) 
        UnitCombatFSM target = GetCurrentTarget();

        // 타켓이 없거나 죽었으면 Idle로 
        if (target == null || !target.IsAlive())
        {
            unit.ChangeState(new IdleState(unit));
            return;
        }

        float distance = Vector3.Distance(unit.transform.position, target.transform.position);

        if (!isSupportTarget)
        {
            float retargetDistance = unit.stats.attackDistance * 3f;
            if (distance > retargetDistance)
            {
                var newTarget = unit.FindNearestEnemy();
                if (newTarget != null && newTarget != target)
                {
                    float newDist = Vector3.Distance(unit.transform.position, newTarget.transform.position);
                    if (newDist < distance)
                    {
                        unit.targetEnemy = newTarget;
                        target = newTarget;
                        distance = newDist;
                    }
                }
            }
        }



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
                unit.ChangeState(new SupportState(unit));
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