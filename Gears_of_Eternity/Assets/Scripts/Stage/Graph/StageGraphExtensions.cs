using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class StageGraphExtensions
{
    public static RuntimeStageNode FindNode(this RuntimeStageGraph g, string id)
        => g.nodes.FirstOrDefault(n => n.nodeId == id);

    public static IEnumerable<RuntimeStageNode> NextNodes(this RuntimeStageGraph g, string id)
    {
        var nextIds = g.edges.Where(e => e.fromNodeId == id).Select(e => e.toNodeId);
        
        foreach (var nid in nextIds)
        {
            yield return g.FindNode(nid);
        }
    }

    public static int MaxLayer(this RuntimeStageGraph g)
        => g.nodes.Count == 0 ? 0 : g.nodes.Max(n => n.layerIndex);
}
