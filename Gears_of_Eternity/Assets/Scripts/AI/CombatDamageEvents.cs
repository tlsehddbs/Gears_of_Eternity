using System;

public enum DamageKind
{
    Unknown = 0,

    Normal = 1,
    Critical = 2,

    Dot_Poison = 10,
    Dot_Bleed = 11,
}

public readonly struct DamagePayload
{
    public readonly float RawDamage;
    public readonly UnitCombatFSM Attacker;
    public readonly DamageKind Kind;

    public DamagePayload(float rawDamage, UnitCombatFSM attacker, DamageKind kind)
    {
        RawDamage = rawDamage;
        Attacker = attacker;
        Kind = kind;
    }

    public static DamagePayload FromLegacy(float rawDamage, UnitCombatFSM attacker)
    {
        return new DamagePayload(rawDamage, attacker, DamageKind.Unknown);
    }
}

public readonly struct DamageResult
{
    public readonly UnitCombatFSM Target;
    public readonly UnitCombatFSM Attacker;

    public readonly DamageKind Kind;

    public readonly float RawDamage;          // 입력 데미지(방어/감소 적용 전)
    public readonly float MitigatedDamage;    // 방어/감소/OnBeforeTakeDamage 적용 후(방어막 적용 전)
    public readonly float BarrierAbsorbed;    // 방어막이 흡수한 양
    public readonly float HpDamage;           // HP에 실제로 들어간 양(방어막 이후)

    public readonly bool IsKilled;

    public DamageResult(
        UnitCombatFSM target,
        UnitCombatFSM attacker,
        DamageKind kind,
        float rawDamage,
        float mitigatedDamage,
        float barrierAbsorbed,
        float hpDamage,
        bool isKilled)
    {
        Target = target;
        Attacker = attacker;
        Kind = kind;
        RawDamage = rawDamage;
        MitigatedDamage = mitigatedDamage;
        BarrierAbsorbed = barrierAbsorbed;
        HpDamage = hpDamage;
        IsKilled = isKilled;
    }
}

public enum HealKind
{
    Unknown = 0,
    Normal = 1,
}

public readonly struct HealPayload
{
    public readonly float RawAmount;
    public readonly UnitCombatFSM Healer;
    public readonly HealKind Kind;

    public HealPayload(float rawAmount, UnitCombatFSM healer, HealKind kind)
    {
        RawAmount = rawAmount;
        Healer = healer;
        Kind = kind;
    }

    public static HealPayload FromLegacy(float rawAmount)
    {
        return new HealPayload(rawAmount, null, HealKind.Unknown);
    }
}

public readonly struct HealResult
{
    public readonly UnitCombatFSM Target;
    public readonly UnitCombatFSM Healer;

    public readonly HealKind Kind;

    public readonly float RawAmount;      // 입력 회복량
    public readonly float AppliedAmount;  // 실제 적용된 회복량(오버힐 제외)
    public readonly float Overheal;       // 오버힐 양

    public HealResult(UnitCombatFSM target, UnitCombatFSM healer, HealKind kind, float rawAmount, float appliedAmount, float overheal)
    {
        Target = target;
        Healer = healer;
        Kind = kind;
        RawAmount = rawAmount;
        AppliedAmount = appliedAmount;
        Overheal = overheal;
    }
}

public readonly struct HpSnapshot
{
    public readonly float CurrentHp;
    public readonly float MaxHp;
    public readonly float Barrier;

    public HpSnapshot(float currentHp, float maxHp, float barrier)
    {
        CurrentHp = currentHp;
        MaxHp = maxHp;
        Barrier = barrier;
    }
}