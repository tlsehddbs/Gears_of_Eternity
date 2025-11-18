using System.Linq;
using UnityEngine;

public class StageGraphUI : MonoBehaviour
{
    public StageGraphLayout layout;
    
    public void Bind(RuntimeStageGraph g)
    {
        layout.Bind(g);

        // 굳이 필요 없는거 같은데?? 나중에 알아보고 삭제할지 유지할지 결정
        var endingLayer = g.MaxLayer();
        var endingNode = g.nodes.FirstOrDefault(n => n.layerIndex == endingLayer);
        
        // if (endingNode != null)
        //     layout.ScrollToCurrent(endingNode.nodeId);
    }

    public void Refresh(RuntimeStageGraph g)
    {
        Bind(g);
    }
}
