using System.Collections.Generic;
using UnityEngine;
using UnitSkillTypes.Enums;
using Combat.Vfx;


// SkillCastVfxManager
// - 스킬 캐스트 VFX를 SkillVfxDatabase 기반으로 재생
// - 회전: Yaw만 추종 + X=-90 보정(항상 유지)
// - 지속시간: DB defaultDuration 기반(외부 hint 미사용)
// - 풀링: Instantiate/Destroy 최소화
public sealed class SkillCastVfxManager : MonoBehaviour
{
    public static SkillCastVfxManager Instance { get; private set; }

    [Header("Database")]
    [SerializeField] private SkillVfxDatabase database; // 너 프로젝트 타입명에 맞춰 수정

    [Header("Duration Clamp")]
    [SerializeField] private float minDuration = 0.6f;
    [SerializeField] private float maxDuration = 2.0f;

    // [Header("Rotation Fix")]
    // [Tooltip("프리팹 축이 옆으로 누워있는 경우 보정용. 기본 -90이 흔함.")]
    // [SerializeField] private Vector3 rotationOffsetEuler = new Vector3(-90f, 0f, 0f);

    [Header("Spam Guard (Optional)")]
    [Tooltip("동일 유닛+동일 스킬 타입 VFX가 너무 자주 나오는 것을 방지. 0이면 비활성.")]
    [SerializeField] private float minIntervalPerType = 0.15f;

    [Header("Pooling")]
    [SerializeField] private Transform poolRoot;

    private readonly Dictionary<GameObject, Queue<GameObject>> pool = new();
    private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new();
    private readonly List<TimedDespawn> timed = new();
    private readonly Dictionary<int, float> lastPlayTimeByKey = new();

    private int nextSpawnId = 1;

    private struct TimedDespawn
    {
        public GameObject go;
        public int spawnId;
        public float despawnAt;
    }

    // 풀링 재사용 중 "이전 예약 despawn"과 충돌 방지용(클래스 중복 충돌 방지 위해 내부 클래스로 둠)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (poolRoot == null)
            poolRoot = transform;
    }

    private void Update()
    {
        float now = Time.time;

        for (int i = timed.Count - 1; i >= 0; i--)
        {
            var t = timed[i];
            if (t.go == null)
            {
                timed.RemoveAt(i);
                continue;
            }

            if (t.despawnAt > now)
                continue;

            var tag = t.go.GetComponent<PooledVfxTag>();
            if (tag == null || tag.SpawnId != t.spawnId)
            {
                timed.RemoveAt(i);
                continue;
            }

            Despawn(t.go);
            timed.RemoveAt(i);
        }
    }

    // durationHint는 받더라도 기본적으로 사용하지 않는다(정책: DB defaultDuration만 사용)
    public void PlayCast(UnitCombatFSM caster, UnitSkillType skillType)
    {
        
        if (caster == null) return;
        if (database == null) return;

        // DB 조회 (너 프로젝트 TryGetCast 시그니처에 맞춰 수정)
        if (!database.TryGetCast(skillType, out var set) || set == null || set.castPrefab == null)
            return;

        
        if(set.castEffect == false) return;

        // 스팸 방지(선택)
        if (minIntervalPerType > 0f)
        {
            int key = MakeSpamKey(caster, skillType);
            if (lastPlayTimeByKey.TryGetValue(key, out float last) && Time.time - last < minIntervalPerType)
                return;

            lastPlayTimeByKey[key] = Time.time;
        }

        // 지속시간: DB defaultDuration 기준
        float duration = ResolveDuration(set.defaultDuration);

        // 위치: (시각 중심 - 루트) + DB offset
        Vector3 visualCenter = GetVisualCenterPosition(caster.transform);
        Vector3 followOffset = (visualCenter - caster.transform.position) + set.offset;

        // 스폰
        GameObject go = Spawn(set.castPrefab);

        // 회전: Yaw만 + XFix(-90) 오프셋 유지
        Quaternion rotOffset = Quaternion.Euler(set.rotationOffsetEuler);

        Quaternion baseRot;
        if (set.followRotation)
        {
            float yaw = caster.transform.eulerAngles.y;
            baseRot = Quaternion.Euler(0f, 0f, 0f);
        }
        else
        {
            baseRot = Quaternion.identity;
        }

        Quaternion finalRot = baseRot * rotOffset;

        go.transform.SetPositionAndRotation(caster.transform.position + followOffset, finalRot);
        go.transform.localScale = Vector3.one * set.scale;

        // 팔로우: followRotation=true이면 Yaw만 추종하되 rotOffset은 항상 유지
        var follow = go.GetComponent<VfxFollow>();
        if (follow == null) follow = go.AddComponent<VfxFollow>();

        // 중요: VfxFollow가 아래 시그니처를 지원해야 함
        // Bind(Transform target, Vector3 offset, bool followYaw, Quaternion rotationOffset)
        follow.Bind(set.followCaster ? caster.transform : null, followOffset, set.followRotation, rotOffset);

        PlayParticles(go);
        ScheduleDespawn(go, duration);
    }

    // -----------------------
    // Duration
    // -----------------------
    private float ResolveDuration(float dbDefaultDuration)
    {
        float d = dbDefaultDuration;

        if (d <= 0f) d = minDuration;

        if (d < minDuration) d = minDuration;
        if (d > maxDuration) d = maxDuration;

        return d;
    }

    // -----------------------
    // Pooling
    // -----------------------
    private GameObject Spawn(GameObject prefab)
    {
        if (!pool.TryGetValue(prefab, out var q))
        {
            q = new Queue<GameObject>();
            pool[prefab] = q;
        }

        GameObject inst = null;
        while (q.Count > 0 && inst == null)
            inst = q.Dequeue();

        if (inst == null)
        {
            inst = Instantiate(prefab);
            instanceToPrefab[inst] = prefab;
        }

        inst.SetActive(true);

        var tag = inst.GetComponent<PooledVfxTag>();
        if (tag == null) tag = inst.AddComponent<PooledVfxTag>();
        tag.MarkSpawn(nextSpawnId++);

        return inst;
    }

    private void ScheduleDespawn(GameObject go, float seconds)
    {
        if (go == null) return;

        var tag = go.GetComponent<PooledVfxTag>();
        if (tag == null) return;

        if (seconds < 0.05f) seconds = 0.05f;

        timed.Add(new TimedDespawn
        {
            go = go,
            spawnId = tag.SpawnId,
            despawnAt = Time.time + seconds
        });
    }

    private void Despawn(GameObject go)
    {
        if (go == null) return;

        if (!instanceToPrefab.TryGetValue(go, out var prefab) || prefab == null)
        {
            Destroy(go);
            return;
        }

        var follow = go.GetComponent<VfxFollow>();
        if (follow != null) follow.Unbind();

        StopParticles(go);

        go.transform.SetParent(poolRoot, false);
        go.SetActive(false);

        if (!pool.TryGetValue(prefab, out var q))
        {
            q = new Queue<GameObject>();
            pool[prefab] = q;
        }

        q.Enqueue(go);
    }

    // -----------------------
    // Particle Control
    // -----------------------
    private static void PlayParticles(GameObject go)
    {
        var systems = go.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            var ps = systems[i];
            ps.Clear(true);
            ps.Play(true);
        }
    }

    private static void StopParticles(GameObject go)
    {
        var systems = go.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            var ps = systems[i];
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    // -----------------------
    // Helpers
    // -----------------------
    private static int MakeSpamKey(UnitCombatFSM caster, UnitSkillType type)
    {
        unchecked { return caster.GetInstanceID() * 397 ^ (int)type; }
    }

    private static Vector3 GetVisualCenterPosition(Transform root)
    {
        var col = root.GetComponentInChildren<Collider>();
        if (col != null) return col.bounds.center;

        var rend = root.GetComponentInChildren<Renderer>();
        if (rend != null) return rend.bounds.center;

        return root.position + Vector3.up * 1.0f;
    }


    // - DB에서 prefab 조회 후, 지정한 월드 좌표에 스폰해서 재생
    // - 캐스터 추적/오프셋 계산을 하지 않음(타격 지점용)
    public void PlayAtWorld(UnitSkillType skillType, Vector3 worldPos, float yaw = 0f, Transform followTransform = null)
    {
        if (database == null) return;

        if (!database.TryGetCast(skillType, out var set) || set == null || set.castPrefab == null)
            return;

        float duration = ResolveDuration(set.defaultDuration);

        GameObject go = Spawn(set.castPrefab);

        Quaternion rotOffset = Quaternion.Euler(set.rotationOffsetEuler);

        // followRotation을 켰고 followTransform이 있으면, 시작 yaw를 타겟 yaw로 맞춰 "첫 프레임 점프"를 방지
        float initialYaw = yaw;
        if (set.followRotation && followTransform != null)
            initialYaw = followTransform.eulerAngles.y;

        Quaternion baseRot = Quaternion.Euler(0f, initialYaw, 0f);
        Quaternion finalRot = baseRot * rotOffset;

        // 위치: 월드 좌표 + DB offset
        Vector3 spawnPos = worldPos + set.offset;
        go.transform.SetPositionAndRotation(spawnPos, finalRot);
        go.transform.localScale = Vector3.one * set.scale;

        // 여기서 핵심:
        // - followCaster=true면 추적 기능 on
        // - followTransform을 넘기면 그 대상을 따라감(상대 유닛 추적)
        ConfigureFollow(go, followTransform, set.followCaster, set.followRotation, rotOffset);

        PlayParticles(go);
        ScheduleDespawn(go, duration);
    }
    private static void ConfigureFollow(GameObject go, Transform followTarget, bool enableFollow, bool followYawOnly, Quaternion rotOffset)
    {
        if (go == null) return;

        var follow = go.GetComponent<VfxFollow>();

        // 추적 비활성 또는 대상 없음: 기존 추적 끊기
        if (!enableFollow || followTarget == null)
        {
            if (follow != null) follow.Unbind();
            return;
        }

        // 추적 활성: 없으면 추가
        if (follow == null) follow = go.AddComponent<VfxFollow>();

        // 현재 스폰 위치를 유지하도록 "상대 오프셋" 계산
        Vector3 offset = go.transform.position - followTarget.position;

        // followYawOnly=true 이면 LateUpdate에서 yaw만 따라가며 rotOffset은 항상 적용됨
        follow.Bind(followTarget, offset, followYawOnly, rotOffset);
    }
}