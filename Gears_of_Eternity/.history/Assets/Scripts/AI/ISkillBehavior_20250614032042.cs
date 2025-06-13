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

public class DashAttackAndGuardSkill : ISkillBehavior
{
    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect skillEffect)
    {
        // 돌진 코루틴 시작 
        caster.StartCoroutine(DashAndAttackRoutine(caster, skillEffect));
    }

    private IEnumerator DashAndAttackRoutine(UnitCombatFSM caster, SkillEffect effect)
    {
        float dashDistance = 30f; //필요 시 SkillEffect/SkillData에 넣고 값으로 사용 
        float dashSpeed = 15f;
        Vector3 dashDir = caster.transform.forward;

        Vector3 targetPos = caster.transform.position + dashDir * dashDistance;
        float dashTime = dashDistance / dashSpeed;
        float t = 0f;

        while (t < dashTime)
        {
            caster.transform.position = Vector3.Lerp(caster.transform.position, targetPos, t / dashTime);
            t += Time.deltaTime;
            yield return null;
        }
        caster.transform.position = targetPos;

        float range = 3.0f; // 돌진 경로 폭 
        Collider[] hits = Physics.OverlapBox(targetPos, new Vector3(range, 1, dashDistance / 2), caster.transform.rotation);

        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<UnitCombatFSM>();
            //유닛인지 체크, 적군 판별 
            if (enemy != null && enemy.unitData.faction != caster.unitData.faction)
            {
                float damage = caster.stats.attack * effect.skillValue;
                enemy.TakeDamage(damage);
                Debug.Log($"[돌진공격] {enemy.name}에게 {damage} 데미지!");
            }
        }

        caster.stats.guardCount += 3;
    }
    public void Remove(UnitCombatFSM caster, SkillEffect effect) { }
}


public class ThrowSpearAttack : ISkillBehavior
{
    public void Execute(UnitCombatFSM caster, UnitCombatFSM target, SkillEffect skillEffect)
}