using UnityEngine;

public class DeadState : UnitState
{
    public DeadState(UnitCombatFSM unit) : base(unit) { }

    public override void Enter()
    {
        unit.Anim_SetMoving(false);
        unit.OnDeath();

        GameObject.Destroy(unit.gameObject);
    }
}