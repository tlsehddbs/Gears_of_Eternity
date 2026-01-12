using System.Collections.Generic;
using UnityEngine;

public class UnitUpgradeVisualController : MonoBehaviour
{
    [Header("Profile")]
    [SerializeField] private UpgradeVisualProfile profile;

    [Header("Targets")]
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Transform auraAnchor;

    [Header("Options")]
    [SerializeField] private bool includeInactiveRenderers = true;
    [SerializeField] private bool applyOnStart = true;

    private UnitCombatFSM unit;
    private MaterialPropertyBlock mpb;
    private GameObject auraInstance;
    private int appliedLevel = int.MinValue;

    // URP Lit: _BaseColor, Standard: _Color
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        unit = GetComponent<UnitCombatFSM>();
        mpb = new MaterialPropertyBlock();

        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(includeInactiveRenderers);

        if (auraAnchor == null)
            auraAnchor = transform;
    }

    private void Start()
    {
        if (!applyOnStart) return;
        RefreshFromUnit();
    }

    // 업그레이드 후 unitData가 바뀌는 지점에서 이 메서드만 호출하면 됨
    public void RefreshFromUnit()
    {
        if (unit == null || unit.unitData == null) return;
        ApplyLevel(unit.unitData.level);
    }

    public void ApplyLevel(int level)
    {
        if (profile == null) return;
        if (appliedLevel == level) return;

        appliedLevel = level;
        var v = profile.GetForLevel(level);

        ApplyMaterial(v);
        ApplyAura(v);
    }

    private void ApplyMaterial(UpgradeVisualProfile.LevelVisual v)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;

            mpb.Clear();

            if (v.applyTint)
            {
                mpb.SetColor(BaseColorId, v.tintColor);
                mpb.SetColor(ColorId, v.tintColor);
            }

            if (v.applyEmission)
            {
                mpb.SetColor(EmissionColorId, v.emissionColor);
            }

            r.SetPropertyBlock(mpb);
        }
    }

    private void ApplyAura(UpgradeVisualProfile.LevelVisual v)
    {
        if (auraInstance != null)
        {
            Destroy(auraInstance);
            auraInstance = null;
        }

        if (v.auraPrefab == null) return;

        auraInstance = Instantiate(v.auraPrefab, auraAnchor);
        auraInstance.transform.localPosition = v.auraLocalOffset;
        auraInstance.transform.localRotation = Quaternion.identity;
        auraInstance.transform.localScale = (v.auraLocalScale == Vector3.zero) ? Vector3.one : v.auraLocalScale;
    }
}