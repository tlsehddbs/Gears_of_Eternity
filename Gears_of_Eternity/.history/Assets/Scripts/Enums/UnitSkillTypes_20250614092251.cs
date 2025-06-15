namespace UnitSkillTypes.Enums
{
    public enum UnitSkillType
    {
        None,                       // 스킬이 없거나 비어있을 경우

        // 공격
        IncreaseAttack,            // 공격력 증가
        IncreaseAttackSpeed,       // 공격 속도 증가
        IncreaseCritChance,        // 치명타 확률 증가
        MultiHit,                  // 연속 공격 / 다타격
        Pierce,                    // 관통 효과
        AreaAttack,                // 범위 공격
        ChainAttack,               // 연쇄 공격
        MarkWeakness,              // 약점 표시 (받는 피해 증가)
        DashAttackAndGuard,        // 돌진 공격 후 피격 방어 / 증기 돌진병 
        ThrowSpearAttack,          // 창 투척 공격 / 기어 창 투척병 
        ConeTripleHit,             // 전방 90도 원뿔형 3연타 / 급속 파열기

        // 방어
        DamageReduction,           // 받는 피해 감소
        IncreaseDefense,           // 방어력 증가
        ShareDefense,              // 아군과 방어 공유
        Shield,                    // 실드 효과

        // 회복
        InstantHeal,               // 즉시 회복
        HealOverTime,              // 지속 회복
        Regen,                     // 재생 효과
        BarrierOnHpHalf,                   // 보호막 / 기동 중장기병 
    

        // 디버프
        Stun,                      // 기절
        Silence,                   // 스킬 사용 불가
        Blind,                     // 실명 (명중률 감소)
        Confuse,                   // 혼란 (적 아군 공격 등)
        Slow,                      // 이동속도 감소
        AttackDown,                // 공격력 감소
        DefenseDown,               // 방어력 감소
        AccuracyDown,              // 명중률 감소
        Poison,                    // 중독, 지속 피해
        Burn,                      // 화염 도트 데미지
        Disarm,                    // 무장 해제, 스킬 비활성화

        // 버프/디버프 조작
        CooldownReduction,         // 쿨타임 감소
        CooldownIncrease,          // 쿨타임 증가
        BuffSpread,                // 버프 범위 확산
        BuffDurationIncrease,      // 버프 지속시간 증가
        RemoveEnemyBuff,           // 적 버프 제거
        RemoveAllEnemyBuffs,       // 전체 버프 제거
        DelayBuff,                 // 지연 발동 버프 

        // 특수/기타
        Taunt,                     // 도발
        SelfDestruct,              // 자폭
        Summon,                    // 유닛 소환
        Revive,                    // 부활
        Clone,                     // 분신 생성
        TimeStop,                  // 시간 정지 (전체 행동 불가)
        RevealStealth,             // 은신 해제
        TeleportReset              // 위치 되돌림
    }
}
