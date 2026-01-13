using System.Collections.Generic;
using UnityEngine;
using UnitSkillTypes.Enums;

public sealed class SkillCastVfxManager : MonoBehaviour
{
    public static SkillCastVfxManager Instance { get; private set; }

    [SerializeField] private SkillVfxDatabase database;

    private readonly Dictionary<GameObject, Queue<GameObject>> pool = new();
    private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new();

    private struct TimedItem
    {
        public GameObject go;
        public int spawnId;
        public float despawnAt;
    }

    private readonly List<TimedItem> timed = new();
    private int nextSpawnId = 1;

    private readonly Dictionary<CastKey, GameObject> activeCast = new();

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
        for (int i = timed.Count - 1; i >= 0; i--)
        {
            var t = timed[i];
            if (t.go == null)
            {
                timed.RemoveAt(i);
                continue;
            }

            if (t.despawnAt > now) continue;

            var tag = t.go.GetComponent<SkillCastPooledVfxTag>();
            if (tag == null || tag.SpawnId != t.spawnId)
            {
                timed.RemoveAt(i);
                continue;
            }

            Despawn(t.go);
            timed.RemoveAt(i);
        }
    }

    public void PlayCast(UnitCombatFSM caster, UnitSkillType type, float castDuration)
    {
        if (caster == null) return;
        if (database == null) return;

        if (!database.TryGetCast(type, out var set) || set == null || set.castPrefab == null)
            return;

        // 같은 캐스터가 같은 스킬을 연속 시전하면 이전 캐스트 VFX를 정리하고 재생
        var key = new CastKey(caster.GetInstanceID(), type);
        if (activeCast.TryGetValue(key, out var prev) && prev != null)
        {
            Despawn(prev);
            activeCast.Remove(key);
        }

        float duration = castDuration;
        if (duration <= 0f) duration = set.defaultDuration;
        if (duration <= 0.01f) duration = 0.25f; // 완전 0이면 너무 짧아서 체감이 없음

        var go = Spawn(set.castPrefab);
        go.transform.position = caster.transform.position + set.offset;
        go.transform.rotation = caster.transform.rotation;
        go.transform.localScale = Vector3.one * set.scale;

        var follow = go.GetComponent<VfxFollow>();
        if (follow == null) follow = go.AddComponent<VfxFollow>();
        follow.Bind(set.followCaster ? caster.transform : null, set.offset, set.followRotation);

        PlayParticles(go);

        activeCast[key] = go;
        ScheduleDespawn(go, duration);
    }

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

        var tag = inst.GetComponent<SkillCastPooledVfxTag>();
        if (tag == null) tag = inst.AddComponent<SkillCastPooledVfxTag>();
        tag.MarkSpawn(nextSpawnId++);

        return inst;
    }

    private void ScheduleDespawn(GameObject go, float seconds)
    {
        if (go == null) return;
        if (seconds <= 0.05f) seconds = 0.05f;

        var tag = go.GetComponent<SkillCastPooledVfxTag>();
        if (tag == null) return;

        timed.Add(new TimedItem
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

        go.SetActive(false);
        go.transform.SetParent(transform, false);

        if (!pool.TryGetValue(prefab, out var q))
        {
            q = new Queue<GameObject>();
            pool[prefab] = q;
        }
        q.Enqueue(go);
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

    private readonly struct CastKey
    {
        private readonly int casterId;
        private readonly UnitSkillType type;

        public CastKey(int casterId, UnitSkillType type)
        {
            this.casterId = casterId;
            this.type = type;
        }
    }
}

public sealed class SkillCastPooledVfxTag  : MonoBehaviour
{
    public int SpawnId { get; private set; }
    public void MarkSpawn(int id) => SpawnId = id;
}

public sealed class VfxFollow : MonoBehaviour
{
    private Transform target;
    private Vector3 offset;
    private bool followRotation;

    public void Bind(Transform t, Vector3 off, bool followRot)
    {
        target = t;
        offset = off;
        followRotation = followRot;
    }

    public void Unbind()
    {
        target = null;
        offset = Vector3.zero;
        followRotation = false;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + offset;
        if (followRotation)
            transform.rotation = target.rotation;
    }
}