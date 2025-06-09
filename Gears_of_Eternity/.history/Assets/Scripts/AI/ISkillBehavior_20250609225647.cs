using System.Collections;
using UnityEngine;

public interface ISkillBehavior
{
    void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect skillEffect);
    void Remove(UnitCombatFSM caster, SkillEffect skillEffect); // 패시브 등 해제용
}

// 즉시 힐 스킬 
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

// 연속 타격 스킬 / 터빈 절삭자 
public class MultiHitSkill : ISkillBehavior
{
    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        if (caster == null || target == null) return;
        caster.StartCoroutine(MultiHitRoutine(caster, target, effect));
    }

    private IEnumerator MultiHitRoutine(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect effect)
    {
        int hitCount = 3;
        float damagePercent = effect.skillValue;  //0.6
        float delay = 0.2f; // 각 타격 간 딜레이

        for (int i = 0; i < hitCount; i++)
        {
            float damage = caster.stats.attack * damagePercent;
            target.TakeDamage(damage);
            // 필요시 이펙트 및 애니메이션 여기에 추가
            yield return new WaitForSeconds(delay);
        }
    }
    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
}

public class BarrierOnHpHalfSkill : ISkillBehavior
{
    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect skillEffect)
    {
        //최대 체력의 25%만큼 방어막 생성
        float barrierAmount = caster.stats.health * skillEffect.skillValue;
        caster.ApplyBarrier(barrierAmount, skillEffect.skillDuration);
        Debug.Log("방어막 발동");

    }
    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
}
