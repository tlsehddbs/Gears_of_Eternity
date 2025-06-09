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












    // 이전 코드
    // public override void Enter()
    // {
    //     var skill = unit.skillData;

    //     switch(skill.skillType)
    //     {
    //         case UnitSkillType.InstantHeal:
    //             unit.targetAlly?.ReceiveHealing(skill.skillValue);
    //             Debug.Log("heal");
    //             break;

    //         case UnitSkillType.IncreaseAttack:
    //             unit.targetAlly?.ApplyBuff(BuffStat.Attack, skill.skillValue, skill.skillDuration);
    //             break;

    //         case UnitSkillType.AttackDown:
    //             unit.targetEnemy?.ApplyDebuff(BuffStat.Attack, skill.skillValue, skill.skillDuration);
    //             break;

    //         default:
    //             Debug.LogWarning($"[지원 스킬 미정의] {skill.skillType}");
    //             break;
    //     }

    //     unit.skillTimer = 0f;
    //     unit.ChangeState(new IdleState(unit));
    //     unit.isProcessingSkill = false; // 상태 완료 → 다시 스킬 사용 허용
    // }

