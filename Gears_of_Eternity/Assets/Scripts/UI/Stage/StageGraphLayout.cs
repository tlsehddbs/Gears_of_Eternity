using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.UI;

public class StageGraphLayout : MonoBehaviour
{
    [Header("References")]
    public RectTransform content;
    public RectTransform nodePrefab;
    public LineRenderer edgePrefab;
    public Transform edgeRoot;

    [Header("Layout")] 
    public float columnWidth = 240f;
    public float columnGap = 80f;
    public float nodeHeight = 90f;
    public float nodeGap = 24f;
    public float topPadding = 60f;
    public float leftPadding = 60f;
    public float rightPadding = 60f;

    [Header("Edges")] 
    [Tooltip("베지어 사용 유무")]
    public bool useBezier = false;
    [Range(0f, 0.5f)] public float bezierBend = 0.25f;
    [Range(8, 64)] public int bezierSegments = 24;
    
    [Header("Z-Depth")]
    [Tooltip("UI 카메라의 Z=0 평면 기준 라인 Z(음수면 카메라 앞). 캔버스/카메라 세팅에 맞춰 조절")]
    public float lineZ = 0f;
    
    private RuntimeStageGraph _graph;
    private int _maxLayer;
    private readonly Dictionary<string, RectTransform> _nodesRectTransform = new();
    private readonly Dictionary<(string from, string to), LineRenderer> _edges = new();
    
    private Canvas _canvas;
    private Camera _uiCamera;
    private ScrollRect _scroll;

    private void Awake()
    {
        _scroll = GetComponentInParent<ScrollRect>();
        _canvas = GetComponentInParent<Canvas>();

        if (_canvas == null)
        {
            Debug.LogError("StageMapLayout: Canvas not found");
        }

        if (_canvas && _canvas.renderMode != RenderMode.ScreenSpaceCamera)
        {
            Debug.LogWarning("Canvas was not Screen Space - Camera");
        }
        
        _uiCamera = _canvas ? _canvas.worldCamera : Camera.main;

        if (edgeRoot == null)
        {
            var r = new GameObject("Edges");
            r.transform.SetParent(_canvas.transform, false);
            edgeRoot = r.transform;
        }
    }

    // ======================= BIND =======================
    public void Bind(RuntimeStageGraph g)
    {
        _graph = g;
        _maxLayer = (_graph.nodes.Count == 0) ? 0 : _graph.nodes.Max(n => n.layerIndex);
        
        int columnCount = _maxLayer + 1;
        float totalWidth = leftPadding + rightPadding + columnCount * columnWidth + (columnCount - 1) * columnGap;
        
        var size = content.sizeDelta;
        content.sizeDelta = new Vector2(totalWidth, size.y);

        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
        
        foreach (var k in _edges)
        {
            if (k.Value)
            {
                Destroy(k.Value.gameObject);
            }
        }
        
        _nodesRectTransform.Clear();
        _edges.Clear();

        for (int l = 0; l <= _maxLayer; l++)
        {
            var layerNodes = _graph.nodes.Where(n => n.layerIndex == l).ToList();
            if (layerNodes.Count == 0)
            {
                continue;
            }

            float x = ColumnCenterX(l);
            
            float totalNodesHeight = layerNodes.Count * nodeHeight + (layerNodes.Count - 1) * nodeGap;
            float startY = totalNodesHeight * 0.5f;

            for (int i = 0; i < layerNodes.Count; i++)
            {
                var n = layerNodes[i];
                var rt = Instantiate(nodePrefab, content);
                rt.name = $"{n.type}Node_{n.layerIndex}-{i}({n.nodeId[..6]})";
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, nodeHeight);
                
                // 확인용 노드 text(추후 이미지로 변경 예정)
                var nt = rt.GetComponentInChildren<TextMeshProUGUI>();
                nt.text = $"{n.type}";
                nt.enableAutoSizing = false;
                nt.fontSize = 26;
                nt.alignment = TextAlignmentOptions.Center;
                
                
                

                float y = -(topPadding + (i * (nodeHeight + nodeGap)) - (startY - nodeHeight * 0.5f));
                rt.anchoredPosition = new Vector2(x, y);

                var btn = rt.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    btn.interactable = n.discovered && !n.completed;
                    string nodeId = n.nodeId;
                    btn.onClick.AddListener(() => StageFlow.Instance.SelectStage(nodeId));
                }
                _nodesRectTransform[n.nodeId] = rt;
            }
        }

        foreach (var e in _graph.edges)
        {
            CreateOrUpdateEdge(e.fromNodeId, e.toNodeId, e.isBridge);
        }
        
        ScrollToCurrent(_graph.currentNodeId);
        //ScrollToLayer(0);
    }

    private void LateUpdate()
    {
        if (_graph == null)
        {
            return;
        }

        foreach (var e in _graph.edges)
        {
            UpdateEdgePosition(e.fromNodeId, e.toNodeId);
        }
    }

    float ColumnCenterX(int logicalLayer)
    {
        int vis = logicalLayer;
        
        return -content.pivot.x * content.rect.width + leftPadding + (columnWidth * 0.5f) + vis * columnWidth + columnGap;
    }

    void CreateOrUpdateEdge(string fromId, string toId, bool isBridge)
    {
        if (!_nodesRectTransform.TryGetValue(fromId, out var from))
        {
            return;
        }

        if (!_nodesRectTransform.TryGetValue(toId, out var to))
        {
            return;
        }

        if (!_edges.TryGetValue((fromId, toId), out var le))
        {
            le = Instantiate(edgePrefab, edgeRoot);
            le.useWorldSpace = true;
            _edges[(fromId, toId)] = le;

            if (isBridge)
            {
                le.widthMultiplier *= 1.3f;
            }
        }
        UpdateEdgePosition(fromId, toId);
    }

    void UpdateEdgePosition(string fromNodeId, string toNodeId)
    {
        if (!_edges.TryGetValue((fromNodeId, toNodeId), out var edge))
        {
            return;
        }

        if (!_nodesRectTransform.TryGetValue(fromNodeId, out var from))
        {
            return;
        }

        if (!_nodesRectTransform.TryGetValue(toNodeId, out var to))
        {
            return;
        }

        Vector3 a = from.TransformPoint((Vector3.zero));
        Vector3 b = to.TransformPoint((Vector3.zero));
        a.z = lineZ;
        b.z = lineZ;

        if (!useBezier)
        {
            edge.positionCount = 2;
            edge.SetPosition(0, a);
            edge.SetPosition(1, b);
        }
        else
        {
            Vector3 dir = (b - a);
            Vector3 right = new Vector3(dir.x, 0f, 0f);
            Vector3 p0 = a;
            Vector3 p2 = b;
            Vector3 p1 = a + right * bezierBend;
            
            edge.positionCount = bezierSegments;

            for (int i = 0; i < bezierSegments; i++)
            {
                float t = i / (bezierSegments - 1f);
                Vector3 p = (1 - t) * (1 - t) * p0 + 2 * (1 - t) * t * p1 + t * t * p2; // 어떻게 작동하는지 모름 ;;
                edge.SetPosition(i, p);
            }
        }
    }

    // TODO: 보스 레이어에 대해서는 적용하지 않도록 변경
    // ======================= REFRESH =======================
    public void Refresh(RuntimeStageGraph g)
    {
        if (g == null)
        {
            return;
        }

        foreach (var node in _graph.nodes)
        {
            if (_nodesRectTransform.TryGetValue(node.nodeId, out var rt))
            {
                UpdateNodeInteract(rt, node);
                UpdateNodeColor(rt, node);
            }
        } 
    }

    public void Refresh() => Refresh(_graph);

    public void UpdateNodeInteract(RectTransform rt, RuntimeStageNode node)
    {
        if (rt == null || node == null)
        {
            return;
        }

        var btn = rt.GetComponentInChildren<Button>(true);
        if (btn != null)
        {
            btn.interactable = node.discovered && !node.completed && !node.locked;
        }
    }

    // 완료한 스테이지에 대해서 노드 색상 업데이트
    // TODO: 노드의 이미지를 어떻게 변경할 것인지 논의한 후 결정할 것
    public void UpdateNodeColor(RectTransform rt, RuntimeStageNode node)
    {
        var img = rt.GetComponentInChildren<Image>(true);
        if (img != null)
        {
            if (node.discovered && node.completed)
            {
                img.color = Color.yellow;
            }
            else
            {
                img.color = Color.white;
            }
        }
    }

    // ======================= SCROLL =======================
    public void ScrollToLayer(int logicalLayer)
    {
        if (_scroll == null)
        {
            return;
        }

        float viewportWidth = _scroll.viewport ? _scroll.viewport.rect.width : ((RectTransform)_scroll.transform).rect.width;
        float contentWidth = content.rect.width;
        float targetX = ColumnCenterX(logicalLayer);

        float normalized = Mathf.Clamp01((targetX - (-content.pivot.x * contentWidth) - viewportWidth * 0.5f) / (contentWidth - viewportWidth));
        _scroll.horizontalNormalizedPosition = normalized;
    }

    // 실제 작동이 안되고 있음. StageGraph 또는 에디터 내 content의 위치 등 다른 외부 요인을 수정해야 할 것 같음
    
    public void ScrollToCurrent(string nodeId, float bias = 0.12f)
    {
        if (_scroll == null)
        {
            return;
        }

        if (!_nodesRectTransform.TryGetValue(nodeId, out var rt))
        {
            return;
        }
        
        Canvas.ForceUpdateCanvases();
        
        float viewportWidth = _scroll.viewport ? _scroll.viewport.rect.width : ((RectTransform)_scroll.transform).rect.width;
        float contentWidth = content.rect.width;
        if (contentWidth <= viewportWidth)
        {
            return;
        }

        Vector3 nodeLocal = content.InverseTransformPoint(rt.position);
        float target = nodeLocal.x + (-content.pivot.x * contentWidth);
        
        float normalized = Mathf.Clamp01((target - viewportWidth * 0.5f) / (contentWidth - viewportWidth));

        normalized = Mathf.Clamp01(normalized - bias);
        
        _scroll.horizontalNormalizedPosition = normalized;
    }
}
