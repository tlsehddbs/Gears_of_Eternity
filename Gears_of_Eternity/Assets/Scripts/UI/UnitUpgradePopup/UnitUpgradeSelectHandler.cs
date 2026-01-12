using UnityEngine;
using UnityEngine.EventSystems;

public class UnitUpgradeSelectHandler : MonoBehaviour, IPointerClickHandler
{
    [Header("Visuals")]
    [SerializeField] private GameObject selectedFx;

    [Tooltip("비활성(골드 부족, 업그레이드 불가) 오버레이.")] 
    [SerializeField] private GameObject disabledOverlay;

    [Tooltip("비활성일 때 카드 전체 알파")] 
    [Range(0.05f, 1f)] 
    [SerializeField] private float disabledAlpha = 0.45f;
    
    [Header("Raycast")]
    [SerializeField] private CanvasGroup canvasGroup;
    
    private UnitUpgradeSceneController _owner;
    public RuntimeUnitCard Card { get; private set; }

    private bool _interactable = true;
    

    public void Setup(UnitUpgradeSceneController owner, RuntimeUnitCard card)
    {
        _owner = owner;
        Card = card;

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

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

        if (disabledOverlay == null)
        {
            var t = transform.Find("DisabledOverlay") ?? transform.Find("LockedOverlay") ?? transform.Find("Unavailable");
            if (t != null)
            {
                disabledOverlay = t.gameObject;
            }
        }
        
        SetSelected(false);
        SetInteractable(true, reasonText: null);
    }

    public void SetInteractable(bool on, string reasonText)
    {
        _interactable = on;
        
        // 입력 차단
        canvasGroup.interactable = on;
        canvasGroup.blocksRaycasts = on;
        
        // 시각처리
        canvasGroup.alpha = on ? 1f : disabledAlpha;
        
        // 선택 효과 비활성화
        if (!on)
        {
            SetSelected(false);
        }
        
        // 오버레이 표시
        if (disabledOverlay != null)
        {
            disabledOverlay.SetActive(on);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_interactable)
        {
            return;
        }
        
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