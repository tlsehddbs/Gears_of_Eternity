using System;
using System.Collections.Generic;
using UnityEngine;
using UnitSkillTypes.Enums;

[CreateAssetMenu(menuName = "Combat/VFX/Skill VFX Database")]
public sealed class SkillVfxDatabase : ScriptableObject
{
    
    [Serializable]
    public sealed class CastVfxSet
    {
        public GameObject castPrefab;
        public Vector3 offset;
        public float scale = 1f;

        // 0 이하이면 호출 측(DelayTime) 또는 파티클 수명 기반으로 처리
        public float defaultDuration = 0f;
        public bool castEffect = false;

        // 시전자 추적(부모로 붙이지 않고 Follow 컴포넌트로 따라가게 함: 풀링 안정)
        public bool followCaster = true;
        public bool followRotation = false;
        public Vector3 rotationOffsetEuler = new Vector3(-90f, 0f, 0f);
    }

    [Serializable]
    public sealed class GroupDefault
    {
        public SkillVfxGroup group;
        public CastVfxSet cast;
    }

    [Serializable]
    public sealed class TypeOverride
    {
        public UnitSkillType skillType;

        // true면 해당 스킬은 VFX를 강제로 끔(패시브/특수 예외 처리)
        public bool disable;

        // 있으면 그룹 대신 이 프리팹 사용
        public CastVfxSet castOverride;

        // Override가 없을 때 그룹을 강제로 지정하고 싶으면 사용
        public SkillVfxGroup groupOverride = SkillVfxGroup.None;
    }

    public List<GroupDefault> groupDefaults = new();
    public List<TypeOverride> overrides = new();

    private Dictionary<SkillVfxGroup, CastVfxSet> groupCache;
    private Dictionary<UnitSkillType, TypeOverride> overrideCache;

    public bool TryGetCast(UnitSkillType type, out CastVfxSet set)
    {
        BuildCacheIfNeeded();

        if (overrideCache.TryGetValue(type, out var ov) && ov != null)
        {
            if (ov.disable)
            {
                set = null;
                return false;
            }

            if (ov.castOverride != null && ov.castOverride.castPrefab != null)
            {
                set = ov.castOverride;
                return true;
            }

            if (ov.groupOverride != SkillVfxGroup.None && groupCache.TryGetValue(ov.groupOverride, out set) && set != null && set.castPrefab != null)
                return true;
        }

        var g = SkillVfxGroupResolver.Resolve(type);
        if (g == SkillVfxGroup.None)
        {
            set = null;
            return false;
        }

        if (groupCache.TryGetValue(g, out set) && set != null && set.castPrefab != null)
            return true;

        set = null;
        return false;
    }

    private void BuildCacheIfNeeded()
    {
        if (groupCache == null)
        {
            groupCache = new Dictionary<SkillVfxGroup, CastVfxSet>();
            foreach (var d in groupDefaults)
            {
                if (d == null || d.cast == null) continue;
                groupCache[d.group] = d.cast;
            }
        }

        if (overrideCache == null)
        {
            overrideCache = new Dictionary<UnitSkillType, TypeOverride>();
            foreach (var o in overrides)
            {
                if (o == null) continue;
                overrideCache[o.skillType] = o;
            }
        }
    }
}