using System.Collections.Generic;
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
    private SceneInstance _stageInstance;
    private bool _hasStage;
    
    private Scene _previousScene;
    private readonly List<GameObject> _prevSceneRoots = new();
    private readonly List<Camera> _prevSceneCameras = new();
    private bool _prevHidden;

    private bool _isExiting;
    //private bool _isLoadingAdditive;

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

    public async Task EnterStageAsync(BaseStageData b, bool hidePreviousScene = true)
    {
        if (_hasStage)
        {
            Debug.LogWarning("[StageRunner] Stage already running");
            return;
        }

        if (b == null || string.IsNullOrEmpty(b.addressableKey))
        {
            Debug.LogError("[StageRunner] Invalid stage data/addressableKey");
            return;
        }

        // _isLoadingAdditive = true;
        _previousScene = SceneManager.GetActiveScene();
        CachePrevScene();

        if (hidePreviousScene)
        {
            SetPrevSceneVisible(false);
            _prevHidden = true;
        }
        else
        {
            _prevHidden = false;
        }

        var handle = Addressables.LoadSceneAsync(b.addressableKey, LoadSceneMode.Additive, activateOnLoad: false);
        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[StageRunner] Load failed: {b.addressableKey} ({handle.Status})");

            if (_prevHidden)
            {
                SetPrevSceneVisible(true);
            }
            // _isLoadingAdditive = false;
            return;
        }

        _loaded = handle;
        _stageInstance = handle.Result;

        var activate = _stageInstance.ActivateAsync();
        while (!activate.isDone)
        {
            await Task.Yield();
        }

        SceneManager.SetActiveScene(_stageInstance.Scene);

        _hasStage = true;
        Debug.Log($"[StageRunner] Stage '{_stageInstance.Scene.name}' loaded (additive)");
    }

    public async Task ExitStageAsync()
    {
        if (_isExiting)
        {
            return;
        }
        _isExiting = true;

        try
        {
            // if (_previousScene.IsValid())
            // {
            //     SceneManager.SetActiveScene(_previousScene);
            // }
            //
            // if (_isLoadingAdditive || _hasStage)
            // {
            //     if (_loaded.HasValue && _loaded.Value.IsValid())
            //     {
            //         var unload = Addressables.UnloadSceneAsync(_loaded.Value, true);
            //         await unload.Task;
            //     }
            //     else
            //     {
            //         if (_stageInstance.Scene.IsValid())
            //         {
            //             var unload = Addressables.UnloadSceneAsync(_stageInstance, true);
            //             await unload.Task;
            //         }
            //         else
            //         {
            //             if (_stageInstance.Scene.IsValid())
            //             {
            //                 await SceneManager.UnloadSceneAsync(_stageInstance.Scene);
            //             }
            //         }
            //     }
            //
            //     _loaded = null;
            //     _hasStage = false;
            //     _isLoadingAdditive = false;
            //
            //     if (_prevHidden)
            //     {
            //         SetPrevSceneVisible(true);
            //         _prevHidden = false;
            //     }
            //
            //     Debug.Log("[StageRunner] Returned to previous scene (additive)");
            //     return;
            // }
            // Debug.LogWarning("[StageRunner] No additive stage to unload. If you used Single mode, load the return scene here");

            if (_previousScene.IsValid())
            {
                SceneManager.SetActiveScene(_previousScene);
            }

            if (_hasStage)
            {
                if (_loaded.HasValue && _loaded.Value.IsValid())
                {
                    var unload = Addressables.UnloadSceneAsync(_loaded.Value, true);
                    await unload.Task;
                }
                else if (_stageInstance.Scene.IsValid())
                {
                    var unload = Addressables.UnloadSceneAsync(_stageInstance, true);
                    await unload.Task;
                }
                else if (_stageInstance.Scene.IsValid())
                {
                    await SceneManager.UnloadSceneAsync(_stageInstance.Scene);
                }
                
                _loaded = null;
                _hasStage = false;

                if (_prevHidden)
                {
                    SetPrevSceneVisible(true);
                    _prevHidden = false;
                }
                
                Debug.Log("[StageRunner] Returned to previous scene (additive)");
            }
        }
        finally
        {
            _isExiting = false;
        }
    }

    private void CachePrevScene()
    {
        _prevSceneRoots.Clear();
        _prevSceneCameras.Clear();

        if (!_previousScene.IsValid())
        {
            return;
        }

        _previousScene.GetRootGameObjects(_prevSceneRoots);
        foreach (var r in _prevSceneRoots)
        {
            var cam = r.GetComponentsInChildren<Camera>(true);
            _prevSceneCameras.AddRange(cam);
        }
    }

    private void SetPrevSceneVisible(bool visible)
    {
        foreach (var cam in _prevSceneCameras)
        {
            if (cam)
            {
                cam.enabled = visible;
            }
        }

        foreach (var root in _prevSceneRoots)
        {
            if (root)
            {
                root.SetActive(visible);
            }
        }
    }
}
