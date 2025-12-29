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
             unit.isProcessingSkill = false;
            unit.ChangeState(new IdleState(unit));
            return;
        }

        float distance = Vector3.Distance(unit.transform.position, target.transform.position);

        //스킬 조건 만족 시 우선 발동
        if (!unit.isProcessingSkill && unit.skillData != null)
        {
            bool used = unit.skillExecutor.TryUseSkillIfPossible(unit, unit.skillData);
            if (used)
            {
                unit.isProcessingSkill = false; // 처리 완료
                return; // 스킬 발동 시 이동/전투 FSM 건너뜀
            }
        }


        //적군 타겟 자동 갱신
        if (!isSupportTarget)
        {
            UnitCombatFSM newTarget = unit.FindNearestEnemy();

            if (newTarget != null && newTarget != target)
            {
                float newDist = Vector3.Distance(unit.transform.position, newTarget.transform.position);
                if (newDist < distance - 0.5f)
                {
                    unit.targetEnemy = newTarget;
                    target = newTarget;
                    distance = newDist;
                }
            }
        }

        //타겟 위치로 이동
        float stopping = unit.agent.stoppingDistance;
        if (distance > stopping + 0.05f)
        {
            unit.agent.SetDestination(target.transform.position);
        }
        else
        {
            unit.agent.ResetPath();
            unit.ChangeState(isSupportTarget ? new SupportState(unit) : new AttackState(unit));
        }
        
        // 이동 도착 시점
        if (distance <= stopping + 0.05f)
        {
            unit.agent.ResetPath();

            // 서포트 목적이면 스킬 시도 후 Idle로
            if (isSupportTarget)
            {
                unit.TryUseSkill(); // 스킬이 사용되든 안되든
                unit.isProcessingSkill = false;
                unit.ChangeState(new IdleState(unit));
            }
            else
            {
                unit.ChangeState(new AttackState(unit));
            }
        }
    }
        

    



        // if (!isSupportTarget)
        // {
        //     float retargetDistance = unit.stats.attackDistance * 3f;
        //     if (distance > retargetDistance)
        //     {
        //         var newTarget = unit.FindNearestEnemy();
        //         if (newTarget != null && newTarget != target)
        //         {
        //             float newDist = Vector3.Distance(unit.transform.position, newTarget.transform.position);
        //             if (newDist < distance)
        //             {
        //                 unit.targetEnemy = newTarget;
        //                 target = newTarget;
        //                 distance = newDist;
        //             }
        //         }
        //     }
        // }
        // float stopping = unit.agent.stoppingDistance;

        // // 타켓이 아직 멀면 계속 따라감 
        // if (distance > stopping + 0.05f)
        // {
        //     unit.agent.SetDestination(target.transform.position);
        // }
        // else
        // {
        //     unit.agent.ResetPath();

        //     //도착-> 서포트/공격 FSM 분기 
        //     if (isSupportTarget)
        //     {
        //         unit.ChangeState(new SupportState(unit));
        //     }
        //     else
        //     {
        //         unit.ChangeState(new AttackState(unit));
        //     }
        // }

    private UnitCombatFSM GetCurrentTarget()
    {
        return isSupportTarget ? unit.targetAlly : unit.targetEnemy;
    }
}