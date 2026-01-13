public class SupportState : UnitState
{
    public SupportState(UnitCombatFSM unit) : base(unit) { }

    public override void Enter()
    {
        unit.Anim_SetMoving(false);

        if (unit.skillData != null)
        {
            unit.skillExecutor.TryUseSkillIfPossible(unit, unit.skillData);
        }

        unit.isProcessingSkill = false;
        unit.ChangeState(new IdleState(unit));
    }

}