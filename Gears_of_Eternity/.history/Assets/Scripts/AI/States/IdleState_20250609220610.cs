using UnityEngine;
using UnitSkillTypes.Enums;

public class IdleState : UnitState
{
    public IdleState(UnitCombatFSM unit) : base(unit){}

    public override void Enter()
    {
        unit.agent.ResetPath();
    }

    public override void Update()
    {
        if (unit.ShouldUseSkill())
        {
            unit.TryUseSkill();
            unit.isProcessingSkill = false;
            return; // 스킬 발동 후 대기 
        }

        
        if (unit.targetEnemy != null && unit.targetEnemy.IsAlive())
            {
                unit.ChangeState(new MoveState(unit, false)); // 공격 타겟팅 
            }
            else
            {
                unit.FindNewTarget();
            }
    }
}
