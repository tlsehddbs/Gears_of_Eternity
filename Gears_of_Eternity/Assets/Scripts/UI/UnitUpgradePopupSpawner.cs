using Unity.Mathematics;
using UnityEngine;

public class UnitUpgradePopupSpawner : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private RectTransform popupRoot;
    [SerializeField] private GameObject upgradePopupPrefab;
    
    [SerializeField] private bool spawnOnStart = true;

    private GameObject _instance;

    private void Start()
    {
        if (StageFlow.Instance.CurrentStageDef.type == StageTypes.StageNodeTypes.Shop)
        {
            return;
        }
        
        // Rest Scene 에서는 시작시 표시되게끔
        if (spawnOnStart)
        {
            SpawnRandom();
        }
    }
    
    public void SpawnRandom()
    {
        Check();
        if (_instance == null) return;

        _instance.transform.SetAsLastSibling();

        var presenter = _instance.GetComponent<UnitUpgradePopupPresenter>();
        if (presenter == null)
        {
            Debug.LogWarning("[UnitUpgradePopupSpawner] UnitUpgradePopupPresenter not found on popup root.");
            return;
        }

        presenter.ShowWithRandomPickedCard();
    }

    public void SpawnForTarget(RuntimeUnitCard target)
    {
        Check();
        if (_instance == null)
        {
            return;
        }
        
        _instance.transform.SetAsLastSibling();

        var presenter = _instance.GetComponent<UnitUpgradePopupPresenter>();
        if (presenter == null)
        {
            Debug.LogWarning("[UnitUpgradePopupSpawner] UnitUpgradePopupPresenter not found on popup root.");
            return;
        }

        presenter.ShowWithTargetCard(target);
    }

    public void Check()
    {
        if (_instance != null)
        {
            return;
        }

        if (popupRoot == null || upgradePopupPrefab == null)
        {
            Debug.LogWarning("[UnitUpgradePopupSpawner] Missing References.");
            return;
        }
        
        _instance = Instantiate(upgradePopupPrefab, popupRoot);
        _instance.transform.SetAsLastSibling(); // 항상 최상단 고정
        //StretchToParent(_instance.transform as RectTransform);
    }

    private static void StretchToParent(RectTransform rt)
    {
        if (rt == null)
        {
            return;
        }
        
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = quaternion.identity;
        rt.anchoredPosition = Vector2.zero;
    }
}
