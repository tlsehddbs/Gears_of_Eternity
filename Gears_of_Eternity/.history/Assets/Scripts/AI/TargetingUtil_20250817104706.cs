using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class TargetingUtil
{
    // ========= 내부 유틸 =========


    /// <summary>
    /// a와 b의 제곱거리(√ 없이 거리 비교) / xzOnly=true면 Y축(높이)을 무시하고 XZ 평면 거리만 사용.
    /// 제곱거리 비교만으로 누가 더 가깝/멀다는 정확히 판단 가능, sqrt 비용을 피함.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float SqrDist(in Vector3 a, in Vector3 b, bool xzOnly)
    {
        if (!xzOnly)
            return (a - b).sqrMagnitude;

        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }

    /// <summary>
    /// 전역 스캔: 씬의 모든 UnitCombatFSM를 가져온다.
    /// - 빈번 호출을 피하고, 가능하면 '사거리 후보 리스트'를 받아 처리하는 API를 쓰는 것을 권장.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static UnitCombatFSM[] GetAllUnits()
    {
        // 프레임 민감 구간에서는 리스트 기반 API를 사용 - 최적화 관련
        return GameObject.FindObjectsByType<UnitCombatFSM>(FindObjectsSortMode.None);
    }


    // ========= 전역(Global) 기준 =========


    /// <summary>
    /// 맵 전체에서 가장 먼 적 개체 1명
    /// - aliveOnly: 죽은 유닛 제외
    /// - xzOnly: XZ 평면 거리만 사용
    /// </summary>
    public static UnitCombatFSM FindFarthestEnemyGlobal(
        UnitCombatFSM caster,
        bool aliveOnly = true,
        bool xzOnly = true)
    {
        if (caster == null) return null;

        UnitCombatFSM best = null;
        float bestSqr = float.NegativeInfinity;
        Vector3 cpos = caster.transform.position;

        var all = GetAllUnits();
        for (int i = 0; i < all.Length; i++)
        {
            var u = all[i];
            if (u == null) continue;
            if (aliveOnly && !u.IsAlive()) continue;
            if (u.unitData.faction == caster.unitData.faction) continue; // 적만

            float d2 = SqrDist(u.transform.position, cpos, xzOnly);
            if (d2 > bestSqr)
            {
                bestSqr = d2;
                best = u;
            }
        }
        return best;
    }

    /// <summary>
    /// 맵 전체에서 가장 가까운 적 개체 1명
    /// </summary>
    public static UnitCombatFSM FindNearestEnemyGlobal(
        UnitCombatFSM caster,
        bool aliveOnly = true,
        bool xzOnly = true)
    {
        if (caster == null) return null;

        UnitCombatFSM best = null;
        float bestSqr = float.PositiveInfinity;
        Vector3 cpos = caster.transform.position;

        var all = GetAllUnits();
        for (int i = 0; i < all.Length; i++)
        {
            var u = all[i];
            if (u == null) continue;
            if (aliveOnly && !u.IsAlive()) continue;
            if (u.unitData.faction == caster.unitData.faction) continue; // 적만

            float d2 = SqrDist(u.transform.position, cpos, xzOnly);
            if (d2 < bestSqr)
            {
                bestSqr = d2;
                best = u;
            }
        }
        return best;
    }




    // ========= 후보 리스트 기반(list) =========




    /// <summary>
    /// 주어진 후보 리스트에서 캐스터 기준 가장 가까운 대상
    /// - enemyOnly: 적만 대상(true), false면 아군/적 모두 대상 (필요시 진영 비교로 아군만 걸러 쓰면 됨)
    /// - aliveOnly: 살아있는 유닛만
    /// - xzOnly: XZ 평면 거리만 사용
    /// </summary>
    public static UnitCombatFSM FindNearestFromList(
        UnitCombatFSM caster,
        IList<UnitCombatFSM> candidates,
        bool enemyOnly = true,
        bool aliveOnly = true,
        bool xzOnly = true)
    {
        if (caster == null || candidates == null || candidates.Count == 0) return null;

        UnitCombatFSM best = null;
        float bestSqr = float.PositiveInfinity;
        Vector3 cpos = caster.transform.position;

        for (int i = 0; i < candidates.Count; i++)
        {
            var u = candidates[i];
            if (u == null) continue;
            if (aliveOnly && !u.IsAlive()) continue;
            if (enemyOnly && u.unitData.faction == caster.unitData.faction) continue;

            float d2 = SqrDist(u.transform.position, cpos, xzOnly);
            if (d2 < bestSqr)
            {
                bestSqr = d2;
                best = u;
            }
        }
        return best;
    }

    /// <summary>
    /// 주어진 후보 리스트에서 캐스터 기준 가장 먼 대상
    /// </summary>
    public static UnitCombatFSM FindFarthestFromList(
        UnitCombatFSM caster,
        IList<UnitCombatFSM> candidates,
        bool enemyOnly = true,
        bool aliveOnly = true,
        bool xzOnly = true)
    {
        if (caster == null || candidates == null || candidates.Count == 0) return null;

        UnitCombatFSM best = null;
        float bestSqr = float.NegativeInfinity;
        Vector3 cpos = caster.transform.position;

        for (int i = 0; i < candidates.Count; i++)
        {
            var u = candidates[i];
            if (u == null) continue;
            if (aliveOnly && !u.IsAlive()) continue;
            if (enemyOnly && u.unitData.faction == caster.unitData.faction) continue;

            float d2 = SqrDist(u.transform.position, cpos, xzOnly);
            if (d2 > bestSqr)
            {
                bestSqr = d2;
                best = u;
            }
        }
        return best;
    }

    // ========= 편의 함수(아군 전용) =========


    // 필요하면 아래 같은 래퍼로 아군 전용 API도 쉽게 만들 수 있음.

    /// <summary>
    /// 후보 리스트에서 가장 가까운 아군 (caster 자신 제외는 excludeSelf=true)
    /// </summary>
    public static UnitCombatFSM FindNearestAllyFromList(
        UnitCombatFSM caster,
        IList<UnitCombatFSM> candidates,
        bool aliveOnly = true,
        bool xzOnly = true,
        bool excludeSelf = true)
    {
        if (caster == null || candidates == null || candidates.Count == 0) return null;

        UnitCombatFSM best = null;
        float bestSqr = float.PositiveInfinity;
        Vector3 cpos = caster.transform.position;

        for (int i = 0; i < candidates.Count; i++)
        {
            var u = candidates[i];
            if (u == null) continue;
            if (excludeSelf && u == caster) continue;
            if (aliveOnly && !u.IsAlive()) continue;
            if (u.unitData.faction != caster.unitData.faction) continue; // 아군만

            float d2 = SqrDist(u.transform.position, cpos, xzOnly);
            if (d2 < bestSqr)
            {
                bestSqr = d2;
                best = u;
            }
        }
        return best;
    }


}
