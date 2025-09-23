using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class StageMapUI : MonoBehaviour
{
    public StageMapLayout layout;
    
    public void Bind(RuntimeStageGraph g)
    {
        layout.Bind(g);

        var enddingLayer = g.MaxLayer();
        var enddingNode = g.nodes.FirstOrDefault(n => n.layerIndex == enddingLayer);
        if(enddingNode != null)
            layout.ScrollToCurrent(enddingNode.nodeId);
    }

    public void Refresh(RuntimeStageGraph g)
    {
        Bind(g);
    }
    

}
