using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public enum GamePhase { OnMap, LoadingStage, InStage, Reward, Transition }

public class StageFlow : MonoBehaviour
{
    public static StageFlow Instance { get; private set; }

    [Header("Assets")] 
    public BaseStageCatalog catalog;

    [Header("Runtime")] 
    public StageGraphGenerator.Rules rules = new();
    public RuntimeStageGraph graph;
    public GamePhase phase = GamePhase.OnMap;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void GenerateNew(int seed)
    {
        graph = StageGraphGenerator.Generate(seed, rules);
        phase = GamePhase.OnMap;
        // TODO: Map UI에 graph 바인딩 및 그리기
    }

    public async void SelectStage(string nextNodeId)
    {
        if (phase != GamePhase.OnMap)
            return;
        
        var next = graph.FindNode(nextNodeId);
        
        if (next == null)
            return;

        // 선택 스테이지 이외의 같은 레이어에 있는 노드에 대한 접근 차단(lock)
        foreach (var ln in graph.nodes)
            if (ln.layerIndex == next.layerIndex && ln.nodeId != next.nodeId)
                ln.locked = true;

        graph.currentNodeId = nextNodeId;
        phase = GamePhase.LoadingStage;

        var def = catalog.GetByType((next.type));
        await StageRunner.Instance.EnterStageAsync(def);
        
        phase = GamePhase.InStage;
    }

    public async Task OnStageCleared()
    {
        if (phase != GamePhase.InStage)
            return;
        
        // 보상
        //phase = GamePhase.Reward;
        // TODO: 보상 연출 (코인 등) 반영

        await StageRunner.Instance.ExitStageAsync();
        
        // 다음 레이어 해금
        var cur = graph.FindNode(graph.currentNodeId);
        
        
        // 임시적으로 completed가 작동하는지 확인
        // TODO: 추후 스테이지 클리어 판별 로직을 추가할 예정
        cur.completed = true;
        
        
        var connectedEdges = graph.edges.Where(e => e.fromNodeId == cur.nodeId);

        foreach (var edge in connectedEdges)
        {
            var nextNode = graph.FindNode(edge.toNodeId);
            if (nextNode != null && !nextNode.discovered)
            {
                nextNode.discovered = true;
            }
        }
        
        // 다음 노드 활성화
        var layout = FindAnyObjectByType<StageMapLayout>();
        if (layout != null)
        {
            layout.Refresh(graph);
        }

        phase = GamePhase.OnMap;
    }
}
