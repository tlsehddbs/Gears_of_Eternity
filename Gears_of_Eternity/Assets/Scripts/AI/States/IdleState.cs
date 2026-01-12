public class IdleState : UnitState
{
    public IdleState(UnitCombatFSM unit) : base(unit){}

    public override void Enter()
    {
        unit.Anim_SetMoving(false);

        if (unit.agent != null)
        {
            unit.agent.ResetPath();
            unit.agent.isStopped = true;
        }
        
        //unit.agent.ResetPath();
    }

    public override void Update()
    {
        
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
