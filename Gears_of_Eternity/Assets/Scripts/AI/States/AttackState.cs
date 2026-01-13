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
        
        // 공격 애니 진행 중이면 상태 전환/거리판단을 과하게 하지 않는 게 안정적
        if (unit.IsAttackAnimInProgress())
        return;

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
        
        // 여기서부터 '전투 회전' 수행 (스킬로 위치가 바뀌어도 매 프레임 보정됨)
        bool facing = unit.RotateTowardsPosition(unit.targetEnemy.transform.position, Time.deltaTime);



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

            if (!facing)
            return;
            
            unit.RequestBasicAttack();
            // 여기서 attackTimer = 0f 하지 않음(중복 리셋 방지)

            // unit.Attack();
            // unit.attackTimer = 0f;
        }
        
    }  
    
    
    
}
