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

    private void SpawnRandom()
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

    private void Check()
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
}
