using System.Linq;
using UnityEngine;

public class StageGraphUI : MonoBehaviour
{
    public StageGraphLayout layout;
    
    public void Bind(RuntimeStageGraph g)
    {
        layout.Bind(g);

        var endingLayer = g.MaxLayer();
        var endingNode = g.nodes.FirstOrDefault(n => n.layerIndex == endingLayer);
        
        if (endingNode != null)
            layout.ScrollToCurrent(endingNode.nodeId);
    }

    public void Refresh(RuntimeStageGraph g)
    {
        Bind(g);
    }
}
