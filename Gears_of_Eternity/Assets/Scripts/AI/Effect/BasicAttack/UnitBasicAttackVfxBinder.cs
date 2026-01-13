using UnityEngine;
using UnitRoleTypes.Enums;

[DisallowMultipleComponent]
public sealed class UnitBasicAttackVfxBinder : MonoBehaviour
{
    [SerializeField] private UnitCombatFSM unit;
    [SerializeField] private UnitVfxAnchors anchors;
    [SerializeField] private BasicAttackVfxProfile profile;

    private bool subscribed;

    private void Reset()
    {
        unit = GetComponent<UnitCombatFSM>();
        anchors = GetComponent<UnitVfxAnchors>();
    }

    private void Awake()
    {
        if (unit == null) unit = GetComponent<UnitCombatFSM>();
        if (anchors == null) anchors = GetComponent<UnitVfxAnchors>();
    }

    private void OnEnable()
    {
        if (subscribed) return;
        if (unit == null) return;

        unit.OnBasicAttackHitFrame += HandleBasicAttackHitFrame;
        subscribed = true;
    }

    private void OnDisable()
    {
        if (!subscribed) return;
        if (unit != null) unit.OnBasicAttackHitFrame -= HandleBasicAttackHitFrame;
        subscribed = false;
    }

    private void HandleBasicAttackHitFrame(UnitCombatFSM target)
    {
        if (BasicAttackVfxManager.Instance == null) return;
        if (profile == null) return;
        if (unit == null || unit.unitData == null) return;

        // 여기 핵심: 원거리 + 서포트 모두 허용
        var bt = unit.unitData.roleType;
        if (bt != RoleTypes.Ranged && bt != RoleTypes.Support)
            return;

        // muzzle이 없으면 바닥에서 쏘는 것처럼 보일 수 있으니 가급적 앵커 세팅 권장
        Transform muzzle = (anchors != null && anchors.muzzle != null) ? anchors.muzzle : unit.transform;

        BasicAttackVfxManager.Instance.PlayRangedBasicAttack(profile, muzzle, target);
    }
}