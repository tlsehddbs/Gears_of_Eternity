using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class StageRunner : MonoBehaviour
{
    public static StageRunner Instance { get; private set; }

    private AsyncOperationHandle<SceneInstance>? _loaded;

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

    public async Task RunStageAsync(BaseStageData b)
    {
        var handle = Addressables.LoadSceneAsync(b.addressableKey, LoadSceneMode.Additive, activateOnLoad: false);
        await handle.Task;

        var scene = handle.Result;
        SceneManager.SetActiveScene(scene.Scene);
        scene.ActivateAsync();

        _loaded = handle;
        // TODO: 스테이지 씬 내부의 Controller가 클리어 시, StageFlow.OnStageCleared() 호출하도록 연결
    }

    public async Task ExitStageAsync()
    {
        if (_loaded.HasValue)
        {
            // 핸들을 넘김, 자동 Release
            await Addressables.UnloadSceneAsync(_loaded.Value, true).Task;
            _loaded = null;
        }
    }
}
