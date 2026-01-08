using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public enum GamePhase { OnMap, LoadingStage, InStage, Reward, Transition }

public class StageFlow : MonoBehaviour
{
    public static StageFlow Instance { get; private set; }
    
    [Header("Assets")] 
    public BaseStageCatalog catalog;
    
    // (optional) 현재 진입한 스테이지 정의를 외부에서 조회할 수 있게
    public BaseStageData CurrentStageDef { get; private set; }

    [Header("Runtime")] 
    private readonly StageGraphGenerator.Rules _rules = new();
    public RuntimeStageGraph graph;
    private readonly LoopController _loopController = new LoopController();
    public GamePhase phase = GamePhase.OnMap;

    [Header("Player")] 
    [SerializeField] private PlayerState playerState;
    private IPlayerProgress PlayerProgress => playerState != null ? playerState : PlayerState.Instance;

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

        if (playerState == null)
        {
            playerState = PlayerState.Instance;
        }
    }

    public void GenerateNew(int seed)
    {
        graph = StageGraphGenerator.Generate(seed, _rules);
        phase = GamePhase.OnMap;
    }

    public async void SelectStage(string nextNodeId)
    {
        if (phase != GamePhase.OnMap)
        {
            return;
        }

        if (graph == null)
        {
            return;
        }

        var next = graph.FindNode(nextNodeId);
        if (next == null)
        {
            return;
        }

        // 선택 스테이지 이외의 같은 레이어에 있는 노드에 대한 접근 차단(lock)
        foreach (var ln in graph.nodes)
        {
            if (ln.layerIndex == next.layerIndex && ln.nodeId != next.nodeId)
            {
                ln.locked = true;
            }
        }

        graph.currentNodeId = nextNodeId;
        phase = GamePhase.LoadingStage;

        var def = catalog.GetByType((next.type));
        CurrentStageDef = def;
        StageContext.Set(def);
        
        await StageRunner.Instance.EnterStageAsync(def);
        
        // 다른 combat scene으로의 이동 시 deck의 꼬임 방지를 위함
        // TODO: 추후 게임에 대해서 최적화 된 방법이 있는지 파악 후 개선할 것 
        //DeckManager.Instance.BuildDeckFromPlayerState(PlayerState.Instance);
        
        Debug.Log($"현재 스테이지 타입 : {CurrentStageDef.type}");
        phase = GamePhase.InStage;
    }

    // ReSharper disable Unity.PerformanceAnalysis
    public async Task OnStageEnd()
    {
        if (phase != GamePhase.InStage)
        {
            return;
        }

        if (graph == null)
        {
            return;
        }
        
        // 루프 테스트용 임시 아이템을 임의로 추가
        // TODO: 나중에 별도로 분리, 다른 스테이지 유형에 유연하게 대응하기 위함
        if(graph.FindNode(graph.currentNodeId).type == StageTypes.StageNodeTypes.Combat)
        {
            PlayerState.Instance.AddItem("GOE_AURA_CORE", 1);
        }
        // TODO: 보상 연출 (코인 등) 반영

        
        var cur = graph.FindNode(graph.currentNodeId);
        // 임시적으로 completed가 작동하는지 확인
        // TODO: 추후 스테이지 클리어 판별 로직을 추가할 예정
        cur.completed = true;
        
        await StageRunner.Instance.ExitStageAsync();
        
        StageContext.Clear();
        CurrentStageDef = null;

        bool loopTriggered = _loopController != null && _loopController.TryGetLoopStarted(graph, cur, PlayerProgress);
        
        var connectedEdges = graph.edges.Where(e => e.fromNodeId == cur.nodeId);

        // 클리어한 Stage(노드)에 연결된 다음 Stage만 Discovered가 true로 변경되도록 제한함.
        foreach (var edge in connectedEdges)
        {
            var nextNode = graph.FindNode(edge.toNodeId);
            if (nextNode != null && !nextNode.discovered && !loopTriggered)
            {
                nextNode.discovered = true;
            }
        }
        
        // 노드 활성화를 위한 레이아웃 새로고침
        var layout = FindAnyObjectByType<StageGraphLayout>();
        if (layout)
        {
            layout.Refresh(graph);
            
            // 노드를 스크롤 중앙으로
            layout.ScrollToCurrent(graph.currentNodeId);
        }
        phase = GamePhase.OnMap;
    }
}
