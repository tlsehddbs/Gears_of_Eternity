using UnityEngine;

[CreateAssetMenu(menuName = "Combat/VFX/Basic Attack VFX Profile")]
public sealed class BasicAttackVfxProfile : ScriptableObject
{
    [Header("Prefabs")]
    public GameObject muzzleFlashPrefab;
    public GameObject tracerPrefab;      
    public GameObject impactPrefab;

    [Header("Lifetime (seconds)")]
    public float muzzleLifetime = 0.25f;
    public float tracerDuration = 0.06f;
    public float impactLifetime = 0.35f;

    [Header("Offsets")]
    public Vector3 muzzleOffset;
    public Vector3 impactOffset; // 기본은 0으로 두는 걸 추천(콜라이더 표면을 쓰기 때문)

    [Header("Scale / Width")]
    public float muzzleScale = 1.0f;
    public float impactScale = 1.0f;
    public float tracerWidth = 0.05f;

    [Header("Options")]
    public bool muzzleFollow = true;
    public bool impactFollow = false;

    [Header("Impact surface nudge")]
    public float surfaceNudge = 0.02f; // 표면에 겹쳐 깜빡이는 것 방지용
}