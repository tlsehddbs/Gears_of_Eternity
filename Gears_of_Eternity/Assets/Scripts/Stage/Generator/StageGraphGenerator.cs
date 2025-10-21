using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class StageGraphGenerator
{
    public sealed class Rules
    {
        public const int Layers = 12;
        public Vector2Int NodeCountRange = new(2, 3);
        public const float BridgeProbability = 0.35f;
        public const int MinChoices = 2;
        public const int MaxChoices = 3;
    }

    public static RuntimeStageGraph Generate(int seed, Rules rules)
    {
        var rand = new System.Random(seed);
        var g = new RuntimeStageGraph {seed = seed};
        
        // 레이어 및 레이어 별 노드 생성
        var layer = new List<List<RuntimeStageNode>>();
        
        for (int l = 0; l < Rules.Layers; l++)
        {
            int count = (l == 0 || l == Rules.Layers - 1) ? 1 : rand.Next(rules.NodeCountRange.x, rules.NodeCountRange.y + 1);

            var layerNodes = new List<RuntimeStageNode>();
            
            for (int i = 0; i < count; i++)
            {
                layerNodes.Add(new RuntimeStageNode
                {
                    nodeId = Guid.NewGuid().ToString("N"),
                    layerIndex = l,
                    type = StageTypes.StageNodeTypes.Combat,
                    discovered = (l < 1)            // 처음 시작시 첫 노드만 활성화 되게끔 변경
                });
#if UNITY_EDITOR
                Debug.Log($"{l} 번 레이어의 {i} 번 노드 생성됨");
#endif
            }
            layer.Add(layerNodes);
            g.nodes.AddRange(layerNodes);
        }
        
        // 노드 연결
        for (int l = 0; l < Rules.Layers - 1; l++)
        {
            var froms = layer[l];
            var tos = layer[l + 1];

            foreach (var f in froms)
            {
                int center = rand.Next(tos.Count);
                var candidates = NeighborIndices(tos.Count, center, 1);
                int k = rand.Next(1, 3);
                
                foreach (var tIndex in PickK(candidates, k, rand))
                {
                    g.edges.Add(new RuntimeStageEdge
                    {
                        fromNodeId = f.nodeId,
                        toNodeId = tos[tIndex].nodeId,
                        isBridge = false
                    });
                }
            }

            foreach (var t in tos)
            {
                bool hasIn = g.edges.Any(e => e.toNodeId == t.nodeId);
                
                if (!hasIn)
                {
                    var f = froms[rand.Next(froms.Count)];
                    g.edges.Add(new RuntimeStageEdge { fromNodeId = f.nodeId, toNodeId = t.nodeId, isBridge = false });
                    
                }
            }
        }

        // 분기 수 보정(현 노드 위치 기준 min-max 보정)
        g.currentNodeId = layer[0][layer[0].Count / 2].nodeId;
        BalanceChoices(g, Rules.MinChoices, Rules.MaxChoices);
        
        // 브릿지 추가(추후 적용할지 의논)
        AddBridges(g, Rules.BridgeProbability, rand);
        
        // 타입 대입
        AssignNodeTypes(g, rand);

        return g;
    }

    static IEnumerable<int> NeighborIndices(int count, int center, int radius)
    {
        for (int i = Math.Max(0, center - radius); i <= Math.Min(count - 1, center + radius); i++)
        {
            yield return i;
        }
    }

    static IEnumerable<int> PickK(IEnumerable<int> items, int k, System.Random rand)
    {
        var arr = items.Distinct().ToList();
        for (int i = 0; i < arr.Count; i++)
        {
            int j = rand.Next(i, arr.Count);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }

        return arr.Take(Mathf.Clamp(k, 0, arr.Count));
    }

    static void BalanceChoices(RuntimeStageGraph g, int min, int max)
    {
        var cur = g.FindNode(g.currentNodeId);
        int nextLayer = cur.layerIndex + 1;
        var nexts = g.NextNodes(cur.nodeId).Where(n => n.layerIndex == nextLayer).ToList();

        if (nexts.Count < min)
        {
            var pool = g.nodes.Where(n => n.layerIndex == nextLayer).Except(nexts).ToList();
            foreach (var add in pool.Take(min - nexts.Count))
            {
                g.edges.Add(new RuntimeStageEdge { fromNodeId = cur.nodeId, toNodeId = add.nodeId, isBridge = false });
            }
        }
        else if (nexts.Count > max)
        {
            int keep = Mathf.Clamp(max, 1, nexts.Count);
            var toRemove = nexts.Skip(keep).ToList();
            g.edges.RemoveAll(e => e.fromNodeId == cur.nodeId && toRemove.Any(n => n.nodeId == e.toNodeId));
        }
    }

    static void AddBridges(RuntimeStageGraph g, float prob, System.Random rand)
    {
        int last = g.MaxLayer();
        for (int l = 0; l <= last; l++)
        {
            if (rand.NextDouble() > prob)
                continue;

            var fromLayer = g.nodes.Where(n => n.layerIndex == l).ToList();
            var toLayer = g.nodes.Where(n => n.layerIndex == l + 1).ToList();
            
            if (fromLayer.Count == 0 || toLayer.Count == 0)
                continue;

            var from = fromLayer[rand.Next(fromLayer.Count)];
            var to = toLayer[rand.Next(toLayer.Count)];

            if (!CreatesCycle(g, from.nodeId, to.nodeId))
                g.edges.Add(new RuntimeStageEdge { fromNodeId = from.nodeId, toNodeId = to.nodeId, isBridge = /*true*/false});
        }
    }

    static bool CreatesCycle(RuntimeStageGraph g, string from, string to)
    {
        var stack = new Stack<string>();
        var visited = new HashSet<string>();
        stack.Push(to);

        while (stack.Count > 0)
        {
            var cur = stack.Pop();
            
            if (cur == from)
                return true;

            if (!visited.Add(cur))
                continue;

            foreach (var e in g.edges.Where(e => e.fromNodeId == cur))
            {
                stack.Push(e.toNodeId);
            }
        }
        
        return false;
    }

    static void AssignNodeTypes(RuntimeStageGraph g, System.Random rand)
    {
        int last = g.MaxLayer();
        
        foreach (var n in g.nodes)
        {
            if (n.layerIndex == last)
            {
                n.type = StageTypes.StageNodeTypes.Boss;
                continue;
            }

            if (n.layerIndex == 0)
            {
                n.type = StageTypes.StageNodeTypes.Combat;
                continue;
            }

            int roll = rand.Next(100);
            
            if (roll < 60)  
                n.type = StageTypes.StageNodeTypes.Combat;
            else if (roll < 75)  
                n.type = StageTypes.StageNodeTypes.Shop;
            else if (roll < 85)  
                n.type = StageTypes.StageNodeTypes.Rest;
            else if (roll < 93)  
                n.type = StageTypes.StageNodeTypes.Event;
            else 
                n.type = StageTypes.StageNodeTypes.None;
        }
    }
}
