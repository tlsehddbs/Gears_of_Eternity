using System.Collections.Generic;
using UnityEngine;

public sealed class BasicAttackVfxManager : MonoBehaviour
{
    public static BasicAttackVfxManager Instance { get; private set; }

    private readonly Dictionary<GameObject, Queue<GameObject>> pool = new();
    private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new();

    // 코루틴 폭증 방지: 중앙 스케줄러
    private struct ReturnItem
    {
        public GameObject go;
        public int spawnId;
        public float despawnAt;
    }

    private readonly List<ReturnItem> returnList = new();
    private int nextSpawnId = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        float now = Time.time;
        for (int i = returnList.Count - 1; i >= 0; i--)
        {
            var item = returnList[i];
            if (item.go == null)
            {
                returnList.RemoveAt(i);
                continue;
            }

            if (item.despawnAt > now) continue;

            var tag = item.go.GetComponent<PooledVfxTag>();
            if (tag == null || tag.SpawnId != item.spawnId)
            {
                returnList.RemoveAt(i);
                continue;
            }

            Despawn(item.go);
            returnList.RemoveAt(i);
        }
    }

    public void PlayRangedBasicAttack(
        BasicAttackVfxProfile profile,
        Transform muzzle,
        UnitCombatFSM target)
    {
        if (profile == null) return;

        Vector3 muzzlePos = muzzle != null ? muzzle.position : Vector3.zero;
        Quaternion muzzleRot = muzzle != null ? muzzle.rotation : Quaternion.identity;

        Vector3 impactPos = ComputeImpactPoint(target, muzzlePos);
        Vector3 dir = (impactPos - muzzlePos);
        if (dir.sqrMagnitude > 0.0001f)
            impactPos += dir.normalized * profile.surfaceNudge;

        // Muzzle
        if (profile.muzzleFlashPrefab != null && muzzle != null)
        {
            Vector3 pos = muzzlePos + profile.muzzleOffset;
            var go = Spawn(profile.muzzleFlashPrefab, pos, muzzleRot, profile.muzzleFollow ? muzzle : null);
            go.transform.localScale = Vector3.one * profile.muzzleScale;

            PlayParticles(go);
            ScheduleDespawn(go, profile.muzzleLifetime);
        }

        // Tracer
        if (profile.tracerPrefab != null && muzzle != null)
        {
            var go = Spawn(profile.tracerPrefab, Vector3.zero, Quaternion.identity, null);
            var tracer = go.GetComponent<TracerLineVfx>();
            if (tracer != null)
            {
                tracer.SetPositions(muzzlePos, impactPos);
                tracer.SetWidth(profile.tracerWidth);
            }

            ScheduleDespawn(go, profile.tracerDuration);
        }

        // Impact
        if (profile.impactPrefab != null)
        {
            var go = Spawn(profile.impactPrefab, impactPos + profile.impactOffset, Quaternion.identity, null);
            go.transform.localScale = Vector3.one * profile.impactScale;

            PlayParticles(go);
            ScheduleDespawn(go, profile.impactLifetime);
        }
    }

    private static Vector3 ComputeImpactPoint(UnitCombatFSM target, Vector3 fromPos)
    {
        if (target == null) return fromPos + Vector3.forward * 3f;

        // 1) 콜라이더 표면
        var col = target.GetComponentInChildren<Collider>();
        if (col != null)
        {
            Vector3 p = col.ClosestPoint(fromPos);
            // fromPos가 콜라이더 내부로 들어가는 특수 케이스 보호
            if ((p - fromPos).sqrMagnitude > 0.0001f)
                return p;

            return col.bounds.center;
        }

        // 2) 렌더러 바운즈 중심(몸통 높이 추정)
        var rend = target.GetComponentInChildren<Renderer>();
        if (rend != null)
            return rend.bounds.center;

        // 3) 최후: 피벗 + 높이 추정
        return target.transform.position + Vector3.up * 1.0f;
    }

    private GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent)
    {
        if (!pool.TryGetValue(prefab, out var q))
        {
            q = new Queue<GameObject>();
            pool[prefab] = q;
        }

        GameObject instance = null;
        while (q.Count > 0 && instance == null)
            instance = q.Dequeue();

        if (instance == null)
        {
            instance = Instantiate(prefab);
            instanceToPrefab[instance] = prefab;
        }

        instance.transform.SetParent(parent, worldPositionStays: parent == null);
        instance.transform.SetPositionAndRotation(pos, rot);
        instance.SetActive(true);

        var tag = instance.GetComponent<PooledVfxTag>();
        if (tag == null) tag = instance.AddComponent<PooledVfxTag>();
        tag.MarkSpawn(nextSpawnId++);

        return instance;
    }

    private void ScheduleDespawn(GameObject instance, float seconds)
    {
        if (instance == null) return;

        if (seconds <= 0f) seconds = 0.1f;

        var tag = instance.GetComponent<PooledVfxTag>();
        if (tag == null) return;

        returnList.Add(new ReturnItem
        {
            go = instance,
            spawnId = tag.SpawnId,
            despawnAt = Time.time + seconds
        });
    }

    private void Despawn(GameObject instance)
    {
        if (instance == null) return;

        if (!instanceToPrefab.TryGetValue(instance, out var prefab) || prefab == null)
        {
            Destroy(instance);
            return;
        }

        StopParticles(instance);

        instance.SetActive(false);
        instance.transform.SetParent(transform, false);

        if (!pool.TryGetValue(prefab, out var q))
        {
            q = new Queue<GameObject>();
            pool[prefab] = q;
        }
        q.Enqueue(instance);
    }

    private static void PlayParticles(GameObject go)
    {
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Clear(true);
            ps.Play(true);
        }
    }

    private static void StopParticles(GameObject go)
    {
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}

public sealed class PooledVfxTag : MonoBehaviour
{
    public int SpawnId { get; private set; }

    public void MarkSpawn(int id)
    {
        SpawnId = id;
    }
}