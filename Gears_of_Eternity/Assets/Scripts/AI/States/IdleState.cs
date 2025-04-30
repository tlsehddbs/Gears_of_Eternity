using UnityEngine;

public class IdleState : UnitState
{
    public IdleState(UnitCombatFSM unit) : base(unit){}

    public override void Enter()
    {
        unit.agent.ResetPath();
    }

    public override void Update()
    {
        if(unit.targetEnemy != null && unit.targetEnemy.IsAlive())
        {
            unit.ChangeState(new MoveState(unit));
        }
        else
        {
            unit.FindNewTarget();
        }
    }
}
