public class SupportState : UnitState
{
    public SupportState(UnitCombatFSM unit) : base(unit) { }

    public override void Enter()
    {
        unit.TryUseSkill();
        unit.isProcessingSkill = false;
        unit.ChangeState(new IdleState(unit));
    }

}