using UnityEngine;

public interface ISkillBehavior
{
    void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect skillEffect);
    void Remove(UnitCombatFSM caster, SkillEffect skillEffect); // 패시브 등 해제용
}

public class InstantHealSkill : ISkillBehavior
{
    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect skillEffect)
    {
        target?.ReceiveHealing(skillEffect.skillValue);
    }
    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
}

public class BuffAttackSkill : ISkillBehavior
{
    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect skillEffect)
    {
        target?.ApplyBuff(BuffStat.Attack, skillEffect.skillValue, skillEffect.skillDuration);
    }
    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
}

public class DebuffAttackSkill : ISkillBehavior
{
    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect skillEffect)
    {
        target?.ApplyDebuff(BuffStat.Attack, skillEffect.skillValue, skillEffect.skillDuration);
    }
    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
}

