using UnityEngine;

public class DeadState : UnitState
{
    public DeadState(UnitCombatFSM unit) : base(unit) { }

    public override void Enter()
    {
        GameObject.Destroy(unit.gameObject);
    }
}