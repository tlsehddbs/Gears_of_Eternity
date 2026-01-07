namespace UnitSkillTypes.Enums
{
    public enum UnitSkillType
    {
        None,                       // 스킬이 없거나 비어있을 경우

        // 공격
        IncreaseAttack,            // 공격력 증가
        IncreaseAttackSpeed,       // 공격 속도 증가
        IncreaseCritChance,        // 치명타 확률 증가
        MultiHit,                  // 연속 공격 / 터빈 절삭자
        Pierce,                    // 관통 효과
        AreaAttack,                // 범위 공격
        ChainAttack,               // 연쇄 공격
        MarkWeakness,              // 약점 표시 (받는 피해 증가)
        DashAttackAndGuard,        // 돌진 공격 후 피격 방어 / 증기 돌진병 
        ThrowSpearAttack,          // 창 투척 공격 / 기어 창 투척병 
        ConeTripleHit,             // 전방 90도 원뿔형 3연타 / 급속 파열기
        BleedBurst,                //최대 8회(0.25초 간격) 1회당 공격력40% + 고정 피해 20% 맞은 적 출혈 시전 중 자신은 방어력 0 // 절삭 기어혼
        DoubleAttack,              // 평타 연속 2회 타격(두 번째 타격은 공격력의 60%) / 기계 난도자

        // 방어
        DamageReduction,           // 받는 피해 감소
        IncreaseDefense,           // 방어력 증가
        ShareDefense,              // 아군과 방어 공유
        Shield,                    // 실드 효과
        ReflectDamage,             // 

        // 회복
        InstantHeal,               // 즉시 회복
        HealOverTime,              // 지속 회복 //키메라 보조체
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
        GrowBuffOverTime,          //시간 비례 성장형 버프 

        // 특수/기타
        Taunt,                     // 도발
        SelfDestruct,              // 자폭
        Summon,                    // 유닛 소환
        Revive,                    // 부활
        Clone,                     // 분신 생성
        TimeStop,                  // 시간 정지 (전체 행동 불가)
        RevealStealth,             // 은신 해제
        TeleportReset,             // 위치 되돌림

        // 새로운 스킬들 중간에 껴 넣으면 설정해둔게 밀림 방지
        PassiveAreaBuff,           //방어력 공유 //기어 공명기
        AttackSpeedUp,             //공속 증가 버프 //자동 발사기
        CriticalStrike,            //지정 대상에게 치명타 + 공격력 +60%의 피해 //초정밀 저격수
        HeatReactiveMark,          // 체력 최상위 적에 표식 → 폭발 // 열선 추적자
        HeavyStrikeAndSlow,        //가장 가까운 적 150% 피해 + 명중 시 5초간 이동속도 30% 감소 //스팀 저격병
        FarthestDoubleAoe,         // 맵 전역: 가장 먼 적에게 1초 간격 원형 AoE 2회(각 160%)    //연산 포격수
        PassiveTurretBarrage,      // 고정 포탑: 3초마다 근접 적 AoE(280%) + 사망 시 자폭(400%) // 기어 포탑병
        QuadFlurryBlind,           // 4연속(각 60%), 4타 명중 시 2초 실명, 쿨타임 7초 //연속 발사기
        HealingAuraDefenseBuff,    // 자기 중심 원형 HoT(3s, 0.2tick, 5%/tick) + 방어력 +20%(8s) //방호 조정사
        EmpowerZoneHighestAttackAlly,  //가장 공격력이 높은 아군 유닛의 위치에 원형 범위의 버프지대 활성화 버프 안에 아군 유닛의 공격력 25%, 공격속도+15%, 이동속도 +15% 증가 버프지대는 7초간 지속 //증기 화력 관제사
        CleanseAndShieldAoE,     //기어 방벽 조정사
        RectStun_ArmorDown, // 가장 가까운 적 방향 직사각형: 기절+방깎 //EMP방출자
        PassiveRegenAndSplitDamage,  // 재생+피해분할(70% 즉시 + 30% DoT 3틱) //ApplyEffectMap에서 패시브 효과 구현 ,지연피해, 재생은 상태효과로 구현 //재생 전투체 
        BleedOnCritPassive, // 치명타 시 출혈 부여 패시브형 //ApplyEffectMap //광증 난도수 
        LifeStealAndKillHealPassive, // 피해의 일부 피흡 + 처치 시 최대체력 비례 회복(패시브) //ApplyEffectMap //포식자
        StackingHasteThenExhaustPassive, // 기본공격 적중마다 스택(공속/이속/받는 피해 증가) → 풀스택 2초 유지 후 초기화 + 공속 디버프 4초 //ApplyEffectMap //광기 절단자
        TripleLayerThinShield, // 3겹 보호막(겹당 MaxHP 12% 1회 흡수) + 겹 파괴 시 1초 20% 피해감소, 8s, CD 16s //골격 강화체
        ThrowSpearPoisonAttack, //원거리 공격 (공격력 1.5배) + 중독(최대 2중첩, 6틱/초, 초당 MaxHP의 5%) //맹독 침두체
        ImmobileAuraBuff, // 이동 불가 + 주변 아군 오라(공격/방어/피감/공속) // 군체 공생체
        RegenNearbyAllies, // 주위 아군 재생 부여 //재생 촉진체
        TargetedAoeBlind, // 적에게 범위형으로 실명 부여 //신경 교란체
        RegenNearbyAlliesUpgrade,  // 근처 아군 3명: 재생(4s) + 해로운효과 제거 + 이속버프
        NearestEnemyAoeStunThenBlind, // 가장 가까운 적 중심 AoE: Stun 2s -> Blind 4s

    }
}
