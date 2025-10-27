using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class RuntimeStageNode
{
    public string nodeId;
    public int layerIndex;
    public StageTypes.StageNodeTypes type;
    public bool discovered;
    public bool completed;
    public bool locked;
}

[Serializable]
public class RuntimeStageEdge
{
    public string fromNodeId;
    public string toNodeId;
    public bool isBridge;
}

[Serializable]
public class RuntimeStageGraph
{
    public List<RuntimeStageNode> nodes = new();
    public List<RuntimeStageEdge> edges = new();
    public string currentNodeId;
    public int seed;
}
