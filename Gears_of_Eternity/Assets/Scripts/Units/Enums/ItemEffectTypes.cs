namespace ItemEffectTypes.Enums
{
    public enum ItemGroupType
    {
        Combat,
        Buff,
        Strategy,
        Passive,
        Consumable
    }
    
    public enum ItemEffectType
    {
        None,
        AttackBuff,         // 공격력
        DefenseBuff,        // 방어력 
        MoveSpeedBuff,      // 이동속도
        CriticalHitBuff,    // 치명타 확률
        HealBuff,           // 체력 회복
        MaxHpBuff,          // 최대 체력
        CostChange,         // 소환 비용
        CooldownReduction,  // 스킬 쿨타임
        DamageAreaBuff,     // 피해 범위
        DurationIncrease,   // 버프 지속 시간
        EnemyDebuff,        // 적 디버프
        EnergyRegen,        // 에너지 회복
        Shield,             // 방어막
        SkillEnhancement,   // 스킬 강화
        CardMaximumChange   // 카드 한도 증가
    }
    
    public enum ItemTriggerConditionType
    {
        Always,               // 항상 발동 (기본 버프)
        OnBattleStart,        // 전투 시작 시
        OnUnitDeath,          // 아군 유닛 사망 시
        OnAttackHit,          // 공격 명중 시
        OnSkillUse,           // 스킬 사용 시
        AreaPresence,         // 특정 지역 내 존재
        SpecificUnitType,     // 특정 유닛 유형만 (ex: 원거리)
        TimePassed,           // 전투 중 특정 시간 경과 후
        HpThreshold,          // 체력 일정 퍼센트 이하일 때
        ComboCount,           // 일정 연속 타격 성공 시
        FirstAttack,          // 전투 시작 후 첫 타격
    }

    public enum ItemTargetType
    {
        None,               // 기본값
        Self,               // 자기 자신
        AllySingle,         // 아군 하나
        AllyAll,            // 아군 전체
        AllyNearby,         // 근처 아군
        AllyTypeSpecific,   // 특정 유형 아군 (근거리/원거리/지원)
        EnemySingle,        // 적 하나
        EnemyAll,           // 적 전체
        EnemyNearby,        // 근처 적군
        EnemyTypeSpecific,  // 특정 유형 적군
        Area,               // 범위 안의 모두 (적+아군 포함 가능)
        RandomEnemy,        // 무작위 적
        RandomAlly,         // 무작위 아군
        SummonedUnit        // 소환된 아군 유닛
    }
    
    public enum EffectValueType
    {
        Percent,
        Increment,
        Decrement,
        Duration,
        Flat
    }
}
