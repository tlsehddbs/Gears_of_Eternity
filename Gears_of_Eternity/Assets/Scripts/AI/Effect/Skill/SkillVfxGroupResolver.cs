using System.Collections.Generic;
using UnitSkillTypes.Enums;

public enum SkillVfxGroup
{
    None,
    Attack,
    Heal,
    Buff,
    Debuff,
    Shield,
    CrowdControl,
    Special,
}

public static class SkillVfxGroupResolver
{
    private static readonly Dictionary<UnitSkillType, SkillVfxGroup> cache = new();

    public static SkillVfxGroup Resolve(UnitSkillType type)
    {
        if (cache.TryGetValue(type, out var g))
            return g;

        g = ResolveByName(type);
        cache[type] = g;
        return g;
    }

    private static SkillVfxGroup ResolveByName(UnitSkillType type)
    {
        var n = type.ToString();

        // 패시브류는 기본적으로 캐스트 연출을 꺼두는 게 안전(스팸 방지)
        if (n.Contains("Passive") || n.Contains("Immobile") || n.Contains("Aura"))
            return SkillVfxGroup.None;

        if (n.Contains("Heal") || n.Contains("Regen"))
            return SkillVfxGroup.Heal;

        if (n.Contains("Barrier") || n.Contains("Shield") || n.Contains("Defense"))
            return SkillVfxGroup.Shield;

        if (n.Contains("Buff") || n.Contains("Increase") || n.Contains("AttackSpeedUp") || n.Contains("Grow"))
            return SkillVfxGroup.Buff;

        if (n.Contains("Poison") || n.Contains("Bleed") || n.Contains("ArmorDown") || n.Contains("Slow") || n.Contains("Exhaust"))
            return SkillVfxGroup.Debuff;

        if (n.Contains("Stun") || n.Contains("Blind") || n.Contains("Silence"))
            return SkillVfxGroup.CrowdControl;

        return SkillVfxGroup.Attack;
    }
}