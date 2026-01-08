using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Object = System.Object;

public class UnitUpgradeSceneController : MonoBehaviour
{
    public event Action CloseRequested;
    public event Action UpgradeApplied;
    
    [Header("PlayerState")] 
    [SerializeField] private PlayerState playerState;

    [Header("UI Slots")] 
    [SerializeField] private RectTransform currentContainer;         // 현재 카드
    
    [SerializeField] private RectTransform optionSlotCenterContainer;    // 업그레이드 옵션 카드 배치 중간
    [SerializeField] private RectTransform optionSlotUpContainer;        // 업그레이드 옵션 카드 배치 위
    [SerializeField] private RectTransform optionSlotDownContainer;      // 업그레이드 옵션 카드 배치 아래
    [SerializeField] private GameObject optionSlotEmptyOverlay;
    
    [Header("Close")]
    [SerializeField] private Button closeButton;

    [Header("Prefabs")]
    [Tooltip("기존 UnitCardPrefab 그대로 넣어주세요. 카드 표시 로직은 프리팹 내부 스크립트를 재사용합니다.")]
    [SerializeField] private GameObject unitCardPrefab;
    
    private RuntimeUnitCard _current;
    private SelectUnitUpgradeHandler _selected;
    
    // shop에서 업그레이드 타깃을 지정
    private RuntimeUnitCard _forcedTarget;
    

    private void Awake()
    {
        if (playerState == null)
        {
            playerState = PlayerState.Instance;
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
        }
    }

    // private void OnEnable()
    // {
    //     
    //     // 지정 업그레이드 유닛이 있으면 최우선으로 적용
    //     if (_forcedTarget != null)
    //     {
    //         BuildUpgradeUI(_forcedTarget);
    //         return;
    //     }
    //     
    //     // 없으면 랜덤
    //     BuildRandomUpgradeUI();
    // }
    
    // TODO: 이 기능을 팝업으로 재활용 하여 상점에서 골드로 플레이어가 유닛을 선택하여 업그레이드를 진행할 수 있도록 변경할 것.
    // TODO: 업그레이드가 불가할 경우에 띄울 팝업은 UpgradeInvalidPanel로 사용할 것

    
    public void OpenRandom()
    {
        if (playerState == null) playerState = PlayerState.Instance;

        if (playerState == null || unitCardPrefab == null)
        {
            Debug.LogWarning("[UnitUpgradeSceneController] Missing refs.");
            return;
        }

        if (!playerState.TryGetRandomUpgradeableCard(out var picked) || picked == null)
        {
            Debug.LogWarning("[UnitUpgradeSceneController] No upgradeable card.");
            return;
        }

        BuildUpgradeUI(picked);
    }

    public void OpenForTarget(RuntimeUnitCard target)
    {
        if (playerState == null) playerState = PlayerState.Instance;

        if (target == null)
        {
            Debug.LogWarning("[UnitUpgradeSceneController] Target is null.");
            return;
        }

        BuildUpgradeUI(target);
    }
 
    // private void BuildRandomUpgradeUI()
    // {
    //     if (playerState == null || unitCardPrefab == null)
    //     {
    //         return;
    //     }
    //
    //     if (!playerState.TryGetRandomUpgradeableCard(out var randomPicked) || randomPicked == null)
    //     {
    //         Debug.LogWarning("[UnitUpgradeSceneController] Upgrade Options is null.");
    //         return;
    //     }
    //     
    //     BuildUpgradeUI(randomPicked);
    // }
    
    public void BuildUpgradeUI(RuntimeUnitCard upgradeTarget)
    {
        // 초기화
        ClearContainer(currentContainer, overlay: null);
        ClearContainer(optionSlotCenterContainer, optionSlotEmptyOverlay);
        ClearContainer(optionSlotUpContainer, optionSlotEmptyOverlay);
        ClearContainer(optionSlotDownContainer, optionSlotEmptyOverlay);

        _current = null;
        _selected = null;
        
        // check references
        if (playerState == null || unitCardPrefab == null || currentContainer == null || 
            optionSlotCenterContainer == null || optionSlotUpContainer == null || optionSlotDownContainer == null)
        {
            Debug.LogWarning("[UnitUpgradeSceneController] References Missing.");
            return;
        }
        
        // TODO: 레벨이 3인 유닛인 경우 업그레이드가 불가하도록 팝업 설정 또는 선택이 불가능하게
        if (upgradeTarget.level > 2 || upgradeTarget.nextUpgradeUnits == null ||
            upgradeTarget.nextUpgradeUnits.Count == 0)
        {
            Debug.LogWarning("[UnitUpgradeSceneController] Target is not upgradeable.");
            return;
        }

        _current = upgradeTarget;
        
        // 현재 카드 표시
        var currentCardGO = Instantiate(unitCardPrefab, currentContainer);
        ResetToContainer(currentCardGO);
        BindCardToView(currentCardGO, _current);
        //currentCardGO.GetComponent<RuntimeUnitCardRef>().SetCard(_current);
        //currentCardGO.GetComponent<CardSlotUI>().Apply(_current);

        var list = _current.nextUpgradeUnits;
        var o0 = (list != null && list.Count > 0) ? list[0] : null;
        var o1 = (list != null && list.Count > 1) ? list[1] : null;
        var o2 = (list != null && list.Count > 2) ? list[2] : null;

        if (o0 != null)
        { 
            SpawnOptionUnit(optionSlotCenterContainer, optionSlotEmptyOverlay, o0);
        }

        if (o1 != null)
        {
            SpawnOptionUnit(optionSlotUpContainer, optionSlotEmptyOverlay, o1);
        }

        if (o2 != null)
        {
            SpawnOptionUnit(optionSlotDownContainer, optionSlotEmptyOverlay, o2);
        }
    }
    
    private void SpawnOptionUnit(RectTransform container, GameObject emptyOverlay, UnitCardData cardData)
    {
        if (emptyOverlay != null)
        {
            emptyOverlay.SetActive(false);
        }

        var toRuntimeData = new RuntimeUnitCard(cardData);

        var go = Instantiate(unitCardPrefab, container);
        ResetToContainer(go);
        BindCardToView(go, toRuntimeData);
        //go.GetComponent<RuntimeUnitCardRef>().SetCard(toRuntimeData);
        //go.GetComponent<CardSlotUI>().Apply(toRuntimeData);

        var selectable = go.GetComponent<SelectUnitUpgradeHandler>();
        if (selectable == null)
        {
            selectable = go.AddComponent<SelectUnitUpgradeHandler>();
        }
        
        selectable.Setup(this, toRuntimeData);
    }
    
    // 단일 클릭 (선택(하이라이트))
    internal void SelectUnitUpgradeOption(SelectUnitUpgradeHandler select)
    {
        if (select == null || select.Card == null)
        {
            return;
        }

        if (_selected != null)
        {
            _selected.SetSelected(false);
        }
        
        _selected = select;
        _selected.SetSelected(true);
    }

    internal void ConfirmSelectedOption(SelectUnitUpgradeHandler select)
    {
        if (_current == null || select == null || select.Card == null)
        {
            return;
        }
        
        bool check = playerState.TryOverwriteDeckCardKeepingUniqueId(_current.uniqueId, select.Card);
        if (!check)
        {
            Debug.LogWarning("[UnitUpgradeSceneController] Cannot confirm selected option.");
            return;
        }
        
        //RequestStageEnd();
        UpgradeApplied?.Invoke();       // presenter가 애니메이션 실행 후 StageEnd 처리
    }

    private static void ClearContainer(Transform container, GameObject overlay)
    {
        if (container != null)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }

        if (overlay != null)
        {
            overlay.SetActive(false);
        }
    }
    
    private static void ResetToContainer(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (!rt)
        {
            return;
        }
        
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    private static void BindCardToView(GameObject go, RuntimeUnitCard card)
    {
        if (go.TryGetComponent<RuntimeUnitCardRef>(out var cardRef))
        {
            cardRef.SetCard(card);
            
            if (go.TryGetComponent<CardSlotUI>(out var slot))
            {
                slot.Apply(card);
            }

            return;
        }

        Debug.LogWarning("[UnitUpgradeSceneController] Cannot bind card to view.");
    }
}





public class SelectUnitUpgradeHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject selectedFx;
    
    private UnitUpgradeSceneController _owner;
    public RuntimeUnitCard Card { get; private set; }

    public void Setup(UnitUpgradeSceneController owner, RuntimeUnitCard card)
    {
        _owner = owner;
        Card = card;

        // 효과 관련 설정 없으면 무시(추후 확장)
        if (selectedFx == null)
        {
            var t = transform.Find("SelectedFx") ?? transform.Find("selected") ?? transform.Find("Highlight");
            if (t != null)
            {
                selectedFx = t.gameObject;
            }
            
            SetSelected(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_owner == null || Card == null)
        {
            return;
        }

        if (eventData.clickCount == 1)
        {
            _owner.SelectUnitUpgradeOption(this);
            return;
        }

        if (eventData.clickCount == 2)
        {
            _owner.SelectUnitUpgradeOption(this);
            _owner.ConfirmSelectedOption(this);
        }
    }

    public void SetSelected(bool on)
    {
        if (selectedFx != null)
        {
            selectedFx.SetActive(on);
        }
    }
}