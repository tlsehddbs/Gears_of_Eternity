using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardUIManager : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    private CanvasGroup _canvasGroup;
    private Canvas _canvas;
    
    private RectTransform _rectTransform;
    private Transform _originalParent;

    public RuntimeUnitCard cardData;

    public CardUIHandler cardUIHandler;

    private Tween currentTween;
    
    
    // 카드가 마우스를 부드럽게 따라가게 만들기 위한 변수
    private Vector3 _targetPosition;

    // 배치 실패시 원래의 Position으로 복귀를 위한 변수
    [SerializeField]
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    
    [SerializeField]
    private Vector3 _originalScale;
    private int _originalSortingOrder;

    public float hoverScale = 1.2f;
    public float hoverMoveY = 50f;

    private bool _isDragging;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        
        _rectTransform = GetComponent<RectTransform>();
        cardUIHandler = GameObject.Find("CardUIHandler").GetComponent<CardUIHandler>();
    }

    public void UpdateOriginalTransform(Vector2 position)
    {
        _originalPosition = position;
        _originalScale = new Vector3(1, 1, 1);  // 고정값
    }
    
    //
    //
    // TODO: 유니티에서 제공하는 DragHandler나 Pointer를 대체할 수 있는 커스텀 hover check 시스템을 만들 것
    //
    // 1. Drag시 카드가 마우스 포인터를 따라갈 때 버벅이는 현상 있음
    //
    //
    
    // Hover
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!GameManager.Instance.isInteractable)
        {
            return;
        }

        OnPointerEnterAnimation();
    }

    private void OnPointerEnterAnimation()
    {
        // if (_canvas != null)
        // {
        //     _originalSortingOrder = _canvas.sortingOrder;
        //     _canvas.overrideSorting = true;
        //     _canvas.sortingOrder = 100; // 다른 카드보다 위에 보이게
        // }

        currentTween?.Kill();
        currentTween = DOTween.Sequence()
            .Join(transform.DOScale(hoverScale, 0.2f).SetEase(Ease.OutBack))
            .Join(transform.DOLocalMoveY(_originalPosition.y + hoverMoveY, 0.2f).SetEase(Ease.OutCubic))
            .SetUpdate(false)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!GameManager.Instance.isInteractable)
        {
            return;
        }
        
        OnPointerExitAnimation(false);
    }

    public void OnPointerExitAnimation(bool instantKillAnim)
    {
        // if (_canvas != null)
        // {
        //     _canvas.sortingOrder = _originalSortingOrder;
        //     _canvas.overrideSorting = false;
        // }
        
        currentTween?.Kill();

        if (instantKillAnim)
        {
            transform.localPosition = _originalPosition;
            transform.localScale = _originalScale;
        }
        else
        {
            currentTween = DOTween.Sequence()
                .Join(transform.DOScale(new Vector3(1, 1, 1), 0.2f).SetEase(Ease.OutQuad))
                .Join(transform.DOLocalMove(_originalPosition, 0.2f).SetEase(Ease.OutCubic))
                .SetUpdate(false)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }
        
    }

    
    // Drag
    public void OnBeginDrag(PointerEventData eventData)
    {
        // TODO: 드래그 시 부모를 해제하고 다른 카드 밑으로 들어가지 않게 가장 위에 표시가 되게끔 변경
        // TODO: Drag시 DOTween의 transform.DOLocalMove 시퀀스를 중지해야 할 듯. 이것때문에 Drag할 때 원래의 자리를 유지하려는 것 처럼 보임. DOTween이 모두 완료된 이후에는 제자지를 찾으려는 움직임이 덜한 것으로 확인됨.
        
        _isDragging = true;
        GameManager.Instance.isDraggingCard = true;
        GameManager.Instance.isInteractable = false;
        
        //_originalParent = transform.parent;
        //transform.SetParent(_canvas.transform);   // UI 최상단으로 올림
        _canvasGroup.blocksRaycasts = false;        // Raycast 차단, 드롭 감지 가능하게
        
        CardMove();
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        CardMove();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        GameManager.Instance.isDraggingCard = false;
        GameManager.Instance.isInteractable = true;
        
        _canvasGroup.blocksRaycasts = true;
        //transform.SetParent(_originalParent);
        
        // UI -> 월드 좌표로 전환
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPoint.z = 0f;

        // 드롭 위치에 콜라이더가 있는지 확인
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            CardDrop cardDrop = hit.collider.GetComponent<CardDrop>();
            if (cardDrop != null)
            {
                DeckManager.Instance.UseCard(cardData);
                UnitSpawnManager.Instance.SpawnUnit(cardData, hit.point);
            }
            cardUIHandler.RefreshHandUI(DeckManager.Instance.hand);
        }
    }

    private void Update()
    {
        if (_isDragging)
        { 
            // Lerp 사용시 OnBeginDrag에서 버벅이는? 현상이 있는듯. 해결 방법을 모르겠음.
            // _rectTransform.position = Vector3.Lerp(_rectTransform.position, _targetPosition, Time.deltaTime * 40f);
            _rectTransform.position = _targetPosition;
        }
    }

    private void CardMove()
    {
        Vector2 localPoint = _rectTransform.localPosition;
        
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                Input.mousePosition,
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
                out localPoint))
        {
            _targetPosition = Input.mousePosition;
        }
    }
}