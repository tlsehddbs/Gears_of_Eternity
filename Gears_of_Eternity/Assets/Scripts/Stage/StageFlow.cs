using System.Linq;
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

        graph.currentNodeId = nextNodeId;
        phase = GamePhase.LoadingStage;

        var def = catalog.GetByType((next.type));
        await StageRunner.Instance.RunStageAsync(def);
        
        phase = GamePhase.InStage;
    }

    public async void OnStageCleared()
    {
        if (phase != GamePhase.InStage)
            return;
        
        // 보상
        phase = GamePhase.Reward;
        // TODO: 보상 연출 (코인 등) 반영

        await StageRunner.Instance.ExitStageAsync();
        
        // 다음 레이어 해금
        var cur = graph.FindNode(graph.currentNodeId);
        
        foreach (var n in graph.nodes.Where(n => n.layerIndex == cur.layerIndex + 1))
        {
            n.discovered = true;
        }

        phase = GamePhase.OnMap;
    }
}
