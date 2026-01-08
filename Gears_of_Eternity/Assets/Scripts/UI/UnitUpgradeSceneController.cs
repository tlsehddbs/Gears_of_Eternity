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
    [Header("PlayerState")] 
    [SerializeField] private PlayerState playerState;

    [Header("UI Slots")] 
    [SerializeField] private RectTransform currentContainer;         // 현재 카드
    
    [SerializeField] private RectTransform optionSlotCenterContainer;    // 업그레이드 옵션 카드 배치 중간
    [SerializeField] private RectTransform optionSlotUpContainer;        // 업그레이드 옵션 카드 배치 위
    [SerializeField] private RectTransform optionSlotDownContainer;      // 업그레이드 옵션 카드 배치 아래
    [SerializeField] private GameObject optionSlotEmptyOverlay;
    

    [Header("Popup")] 
    
    
    [Header("Close")]
    [SerializeField] private Button closeButton;

    [Header("Prefabs")]
    [Tooltip("기존 UnitCardPrefab 그대로 넣어주세요. 카드 표시 로직은 프리팹 내부 스크립트를 재사용합니다.")]
    [SerializeField] private GameObject unitCardPrefab;

    
    private RuntimeUnitCard _current;
    private SelectUnitUpgradeHandler _selected;
    

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(RequestStageEnd);
        }
    }

    private void OnEnable()
    {
        BuildRandomUpgradeUI();
    }
    
    // TODO: 이 기능을 팝업으로 재활용 하여 상점에서 골드로 플레이어가 유닛을 선택하여 업그레이드를 진행할 수 있도록 변경할 것.
    // TODO: 업그레이드가 불가할 경우에 띄울 팝업은 UpgradeInvalidPanel로 사용할 것

    public void BuildRandomUpgradeUI()
    {
        if (playerState == null)
        {
            playerState = PlayerState.Instance;
        }
        
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
        
        // 랜덤 카드 선택
        if (!playerState.TryGetRandomUpgradeableCard(out _current) || _current == null)
        {
            Debug.LogWarning("[UnitUpgradeSceneController] Upgrade Options is null.");
            return;
        }
        
        // 현재 카드 표시
        var currentCardGO = Instantiate(unitCardPrefab, currentContainer);
        ResetToContainer(currentCardGO);
        currentCardGO.GetComponent<RuntimeUnitCardRef>().SetCard(_current);
        currentCardGO.GetComponent<CardSlotUI>().Apply(_current);

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
        go.GetComponent<RuntimeUnitCardRef>().SetCard(toRuntimeData);
        go.GetComponent<CardSlotUI>().Apply(toRuntimeData);

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
        
        RequestStageEnd();
    }

    private async void RequestStageEnd()
    {
        try
        {
            await StageFlow.Instance.OnStageEnd();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
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
            
            Debug.Log(this.Card.uniqueId);
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