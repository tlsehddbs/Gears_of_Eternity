using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DeckListViewOnShop : MonoBehaviour
{
    [Header("PlayerState")] 
    [SerializeField] private PlayerState playerState;

    [Header("UI")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject cardPrefab;
    
    [Header("Popup")]
    [SerializeField] private UnitUpgradePopupSpawner popupSpawner;

    private readonly List<GameObject> _spawned = new();

    private void Awake()
    {
        if (playerState == null)
        {
            playerState = PlayerState.Instance;
        }

        if (popupSpawner == null)
        {
            popupSpawner = FindAnyObjectByType<UnitUpgradePopupSpawner>();
        }
    }

    private void OnEnable()
    {
        if (playerState != null)
        {
            // playerState.OnDeckChanged를 구독해서 Deck 변경 시 자동 갱신하도록
            playerState.OnDeckChanged += ReBuild;
            playerState.OnGoldChanged += OnGoldChanged;
        }
        
        ReBuild();
    }

    private void OnDisable()
    {
        if (playerState != null)
        {
            playerState.OnDeckChanged -= ReBuild;
            playerState.OnGoldChanged -= OnGoldChanged;
        }
    }

    private void OnGoldChanged(int _) => RefreshLocks();    // 골드 상태가 바뀌면 상태 다시 계산 및 적용 

    public void ReBuild()
    {
        if (playerState == null || content == null || cardPrefab == null)
        {
            Debug.LogWarning("[DeckListWViewOnShop] Missing references.");
            return;
        }

        ClearAll();

        var deck = playerState.DeckCards;
        if (deck == null || deck.Count == 0)
        {
            return;
        }

        for (int i = 0; i < deck.Count; i++)
        {
            var card = deck[i];
            if (card == null)
            {
                return;
            }
            
            var go = Instantiate(cardPrefab, content);
            _spawned.Add(go);
            
            // 데이터 바인딩
            if (go.TryGetComponent<RuntimeUnitCardRef>(out var cardRef))
            {
                cardRef.SetCard(card);
            }
            else
            {
                Debug.LogWarning("[DeckListWViewOnShop] Card prefab has no binder. (RuntimeUnitCardRef)");
            }

            var dbc = go.GetComponent<OpenUnitUpgradePopupOnDoubleClick>();
            if (dbc == null)
            {
                dbc = go.AddComponent<OpenUnitUpgradePopupOnDoubleClick>();
            }
            
            // 스포너 주입
            dbc.SetSpawner(popupSpawner);

            ApplyLock(go, card);
        }

        RefreshLocks();
    }

    private void RefreshLocks()
    {
        if (playerState == null)
        {
            return;
        }

        for (int i = 0; i < _spawned.Count; i++)
        {
            var go = _spawned[i];
            if (go == null)
            {
                continue;
            }

            RuntimeUnitCard card = null;

            if (go.TryGetComponent<RuntimeUnitCardRef>(out var cardRef))
            {
                card = cardRef.Card;
            }

            if (card == null)
            {
                continue;
            }

            ApplyLock(go, card);
        }
    }

    private void ApplyLock(GameObject go, RuntimeUnitCard card)
    {
        bool upgradeable = card.level < 3 && card.nextUpgradeUnits != null && card.nextUpgradeUnits.Count > 0;
        
        int price = upgradeable
            ? UnitUpgradePriceCalculator.GetUpgradePrice(card.level, playerState.UpgradeCount + 1,StageTypes.StageNodeTypes.Shop)
            : int.MaxValue;

        bool locked = !upgradeable || playerState.Gold < price;
        
        ShopCardLockUtil.Apply(go, locked);
    }

    private void ClearAll()
    {
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            if (_spawned[i] != null)
            {
                Destroy(_spawned[i]);
            }
        }
        _spawned.Clear();
    }
}
