public interface ISkillBehavior
{
    void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillData skillData);
}

public class InstantHealSkill : ISkillBehavior
{
    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillData skillData)
    {
        target?.ReceiveHealing(skillData.skillValue);
    }
}

public class BuffAttackSkill : ISkillBehavior
{
    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillData skillData)
    {
        target?.ApplyBuff(BuffStat.Attack, skillData.skillValue, skillData.skillDuration);
    }
}

public class DebuffAttackSkill : ISkillBehavior
{
    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillData skillData)
    {
        target?.ApplyDebuff(BuffStat.Attack, skillData.skillValue, skillData.skillDuration);
    }
}