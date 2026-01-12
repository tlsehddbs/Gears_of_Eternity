using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeVisualProfile", menuName = "Visual/Upgrade Visual Profile")]
public class UpgradeVisualProfile : ScriptableObject
{
    [Serializable]
    public struct LevelVisual
    {
        [Min(0)] public int level;

        [Header("Tint")]
        public bool applyTint;
        public Color tintColor;

        [Header("Emission")]
        public bool applyEmission;
        [ColorUsage(true, true)] public Color emissionColor;

        [Header("Aura VFX")]
        public GameObject auraPrefab;
        public Vector3 auraLocalOffset;
        public Vector3 auraLocalScale;
    }

    [Header("Default (when no match)")]
    public LevelVisual defaultVisual;

    [Header("Overrides")]
    public List<LevelVisual> visualsByLevel = new List<LevelVisual>();

    public LevelVisual GetForLevel(int level)
    {
        LevelVisual best = defaultVisual;
        int bestLv = int.MinValue;

        for (int i = 0; i < visualsByLevel.Count; i++)
        {
            var v = visualsByLevel[i];
            if (v.level <= level && v.level > bestLv)
            {
                best = v;
                bestLv = v.level;
            }
        }

        return best;
    }
}