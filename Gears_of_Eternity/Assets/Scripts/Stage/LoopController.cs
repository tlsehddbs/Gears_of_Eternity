using System;
using System.Linq;
using UnityEngine;

public class LoopController
{
    private readonly LoopRule _rule = new LoopRule
    {
        enabled = true,                    // 가본 상태는 false, Loop가 시작되었을 때부터 enabled를 true로 변경
        requiredItemId = "GOE_AURA_CORE",   // 아이템 아이디는 임시로 작성해 둔 것임
        requiredItemCount = 5
    };
    
    // ReSharper disable Unity.PerformanceAnalysis
    public bool TryGetLoopStarted(RuntimeStageGraph g, RuntimeStageNode rn, IPlayerProgress p)
    {
        if (!_rule.enabled || g == null || rn == null)
        {
            return false;
        }
        
        // TODO: p가 현재 null인 상태로 IGetPlayerProgress를 구현후 문제를 수정할 것
        if ( /*p == null ||*/ string.IsNullOrEmpty(_rule.requiredItemId) || _rule.requiredItemCount <= 0)
        {
            return false;
        }
        
        int preBossLayer = g.nodes.Max(n => n.layerIndex) - 1;
        if (rn.layerIndex != preBossLayer)
        {
            return false;
        }

        // int count = p.GetItemCount(rule.requiredItemId);
        // if (count >= rule.requiredItemCount)
        //     return false;

        var returnNode = FindReturnNode(g, preBossLayer);
        if (returnNode == null)
        {
#if UNITY_EDITOR
            Debug.LogError("[LoopController] 회귀 대상 노드 찾기 실패");
#endif
            return false;
        }
        
        // 구간 리셋
        ResetAndWrap(g, preBossLayer, returnNode);
        return true;
    }

    private RuntimeStageNode FindReturnNode(RuntimeStageGraph g, int preBossLayer)
    {
        var singleCombatCandidates = g.nodes
            .GroupBy(n => n.layerIndex)
            .Select(gr => new { layer = gr.Key, nodes = gr.ToList() })
            .Where(x => x.nodes.Count == 1 && x.nodes[0].type == StageTypes.StageNodeTypes.Combat && x.layer != 0)
            .Select(x => x.nodes[0])
            .ToList();

        // TODO: 추후 적당한 부분으로 이동시키도록 변경 예정
        if (singleCombatCandidates.Count != 0)
        {
            return singleCombatCandidates[UnityEngine.Random.Range(0, singleCombatCandidates.Count)];
        }

        // 노드가 1개이면서 combat인 노드가 없을 경우 comabat인 다른 노드로 회귀하기 위한 fallback 구현부
        var fallbackNode = g.nodes.Where(n => n.type != StageTypes.StageNodeTypes.Boss).ToList();
        if (fallbackNode.Count > 0)
        {
            return fallbackNode[UnityEngine.Random.Range(0, fallbackNode.Count)];
        }
        // when fallback is fail, return to first Node
        return g.nodes.FirstOrDefault();
    }

    private void ResetAndWrap(RuntimeStageGraph g, int preBossLayer, RuntimeStageNode returnNode)
    {
        int fromLayer = returnNode.layerIndex;
        int toLayer = preBossLayer;

        foreach (var n in g.nodes)
        {
            if (n.layerIndex < fromLayer || n.layerIndex > toLayer)
            {
                continue;
            }

            n.completed = false;
            n.discovered = false;
            n.locked = false;
        }
        
        // 회귀할 노드만 활성화
        returnNode.discovered = true;
        returnNode.locked = false;

        // 같은 레이어상에 다른 노드가 있을 경우 잠금
        foreach (var m in g.nodes)
        {
            if (m.layerIndex == returnNode.layerIndex && m.nodeId != returnNode.nodeId)
            {
                m.discovered = false;
                m.locked = true;
            }
        }
        g.currentNodeId = returnNode.nodeId;
    }
}


[Serializable]
public class LoopRule
{
    public bool enabled = true;

    public string requiredItemId;
    public int requiredItemCount = 1;
}
