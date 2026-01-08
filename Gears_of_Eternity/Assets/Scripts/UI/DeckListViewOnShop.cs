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

    [Header("Options")] 
    [SerializeField] private bool rebuildOnEnable;

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
        }

        if (rebuildOnEnable)
        {
            ReBuild();
        }
    }

    private void OnDisable()
    {
        if (playerState != null)
        {
            playerState.OnDeckChanged -= ReBuild;
        }
    }

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
        }
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
