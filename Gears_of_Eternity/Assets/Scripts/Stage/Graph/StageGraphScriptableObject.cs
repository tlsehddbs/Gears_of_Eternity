using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StageNodeScriptableObject
{
    public string nodeId;
    public int layerIndex;
    public Vector2Int layoutPosition;
    public StageTypes.StageNodeTypes type;
}

[Serializable]
public class StAgeEdgeScriptableObject
{
    public string fromNodeId;
    public string toNodeId;
    public bool isBridge;
}

[CreateAssetMenu(menuName = "Create New Stage Graph")]
public class StageGraphScriptableObject : ScriptableObject
{
    public List<StageNodeScriptableObject> nodes = new();
    public List<StAgeEdgeScriptableObject> edges = new();
}
