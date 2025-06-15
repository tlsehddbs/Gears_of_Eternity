using UnityEngine;
using UnitSkillTypes.Enums;
public class AttackState : UnitState
{
    public AttackState(UnitCombatFSM unit) : base(unit){}

    public override void Update()
    {
        unit.attackTimer += Time.deltaTime;

        if (unit.targetEnemy == null || !unit.targetEnemy.IsAlive())
        {
            unit.ChangeState(new IdleState(unit));
            return;
        }

        float dist = Vector3.Distance(unit.transform.position, unit.targetEnemy.transform.position);
        if (dist > unit.agent.stoppingDistance + 0.05f)
        {
            unit.ChangeState(new MoveState(unit, false));
            return;
        }

        //  스킬 우선 체크
        if (!unit.isProcessingSkill && unit.skillData != null)
        {
            bool used = unit.skillExecutor.TryUseSkillIfPossible(unit, unit.skillData);
            if (used)
            {
                unit.isProcessingSkill = false;
                return; // 스킬 발동 시 일반 공격은 건너뜀
            }
        }

        if (unit.attackTimer >= unit.stats.attackSpeed)
        {
            unit.Attack();
            unit.attackTimer = 0f;
        }
    }  
    
}
