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
    
    
    // 카드가 마우스를 부드럽게 따라가게 만들기 위한 변수
    private Vector3 _targetPosition;

    // 배치 실패시 원래의 Position으로 복귀를 위한 변수
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    
    private Vector3 _originalScale;
    private int _originalSortingOrder;

    public float hoverScale = 1.2f;
    public float hoverMoveY = 50f;

    private bool _isDragging;
    private bool _isHovering;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        
        _rectTransform = GetComponent<RectTransform>();
        cardUIHandler = GameObject.Find("CardUIHandler").GetComponent<CardUIHandler>();
    }

    public void UpdateOriginalTransform()
    {
        _originalPosition = transform.localPosition;
        _originalScale = transform.localScale;
    }
    
    //
    //
    // TODO: 유니티에서 제공하는 DragHandler나 Pointer를 대체할 수 있는 커스텀 hover check 시스템을 만들 것
    //
    // 1. 마우스는 가만히 있는 상태, 카드가 움직이는 상황에서 DOTween이 정상적으로 작동하지 않을 가는ㅇ성 높음
    // 2. Drag시 카드가 마우스 포인터를 따라갈 때 버벅이는 현상 있음
    //
    //
    
    // Hover
    public void OnPointerEnter(PointerEventData eventData)
    {
        // _originalPosition = transform.localPosition;
        // _originalScale = transform.localScale;
        if(GameManager.Instance.isHoveringCard || GameManager.Instance.isDraggingCard) 
            return;
        
        _isHovering = true;
        GameManager.Instance.isHoveringCard = true;
        
        if (_canvas != null)
        {
            _originalSortingOrder = _canvas.sortingOrder;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 100; // 다른 카드보다 위에 보이게
        }

        transform.DOScale(hoverScale, 0.2f).SetEase(Ease.OutBack);
        transform.DOLocalMoveY(_originalPosition.y + hoverMoveY, 0.2f).SetEase(Ease.OutCubic);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GameManager.Instance.isHoveringCard = false;
        Sequence seq = DOTween.Sequence();

        seq.Join(transform.DOScale(new Vector3(1, 1, 1), 0.2f).SetEase(Ease.OutQuad));
        seq.Join(transform.DOLocalMove(_originalPosition, 0.2f).SetEase(Ease.OutCubic));
        seq.OnComplete(() => _isHovering = false );
        
        // transform.DOScale(_originalScale, 0.2f).SetEase(Ease.OutQuad);
        // transform.DOLocalMove(_originalPosition, 0.2f).SetEase(Ease.OutCubic);

        if (_canvas != null)
        {
            _canvas.sortingOrder = _originalSortingOrder;
            _canvas.overrideSorting = false;
        }
    }

    
    // Drag
    public void OnBeginDrag(PointerEventData eventData)
    {
        //transform.DOKill();
        DOTween.Kill(this);
        
        // TODO: Drag시 DOTween의 transform.DOLocalMove 시퀀스를 중지해야 할 듯. 이것때문에 Drag할 때 원래의 자리를 유지하려는 것 처럼 보임. DOTween이 모두 완료된 이후에는 제자지를 찾으려는 움직임이 덜한 것으로 확인됨.
        
        _isHovering = false;
        _isDragging = true;
        GameManager.Instance.isHoveringCard = false;
        GameManager.Instance.isDraggingCard = true;
        
        _originalPosition = _rectTransform.localPosition;
        _originalRotation = _rectTransform.localRotation;
        
        //_originalParent = transform.parent;
        //transform.SetParent(_canvas.transform);   // UI 최상단으로 올림
        _canvasGroup.blocksRaycasts = false;      // Raycast 차단, 드롭 감지 가능하게
        
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
        
        _canvasGroup.blocksRaycasts = true;
        //transform.SetParent(_originalParent);
        
        _rectTransform.DOAnchorPos(_originalPosition, 0.3f).SetEase(Ease.OutExpo);
        _rectTransform.DOLocalRotateQuaternion(_originalRotation, 0.3f);
        
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
                cardUIHandler.RefreshHandUI(DeckManager.Instance.hand);
            }
        }
    }

    private void Update()
    {
        //Debug.Log($"{_rectTransform.position}      //////     {Input.mousePosition}");
        _targetPosition = Input.mousePosition;
        
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
            _targetPosition= localPoint;
        }
    }
}