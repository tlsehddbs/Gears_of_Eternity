using UnityEngine;

public class AttackState : UnitState
{
    public AttackState(UnitCombatFSM unit) : base(unit) { }

    public override void Update()
    {
        if (unit.IsStunned())
        {
            unit.ChangeState(new IdleState(unit)); // 즉시 공격 루프 탈출
            return;
        }        
        
        unit.attackTimer += Time.deltaTime;
        // 기본 타겟이 없거나 죽었으면 Idle 
        if (unit.targetEnemy == null || !unit.targetEnemy.IsAlive())
        {
            unit.ChangeState(new IdleState(unit));
            return;
        }

        //평타 비활성화면 Idle 상태로
        if (unit.disableBasicAttack)
        {
            unit.ChangeState(new IdleState(unit));
            return;
        }
        
        // 범위 밖이면 다시 이동 
        float dist = Vector3.Distance(unit.transform.position, unit.targetEnemy.transform.position);
        if (dist > unit.agent.stoppingDistance + 0.05f)
        {
            unit.ChangeState(new MoveState(unit, false));
            return;
        }
        if (unit.unitData.roleType == UnitRoleTypes.Enums.RoleTypes.Support && unit.CanUseSkill() && unit.skillData != null)
        {
            foreach (var effect in unit.skillData.effects)
            {
                var behavior = SkillExecutor.GetSkillBehavior(effect.skillType);
                if (behavior != null && behavior.ShouldTrigger(unit, effect))
                {
                    unit.isProcessingSkill = true;
                    unit.agent.ResetPath();
                    unit.ChangeState(new MoveState(unit, true)); //아군 타겟으로 이동 
                    return;
                }
            }
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

        // 일반 공격
        if (unit.attackTimer >= unit.stats.attackSpeed)
        {
            unit.Attack();
            unit.attackTimer = 0f;
        }
        
    }  
    
    
    
}
