using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    private CanvasGroup _canvasGroup;
    private Canvas _canvas;
    
    private RectTransform _rectTransform;
    private Transform _originalParent;

    public RuntimeUnitCard cardData;

    public HandCurveUI handCurveUI;
    
    
    // 카드가 마우스를 부드럽게 따라가게 만들기 위한 변수
    private Vector3 _targetPosition;

    // 배치 실패시 원래의 Position으로 복귀를 위한 변수
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    
    private Vector3 _originalScale;
    private int _originalSortingOrder;

    public float hoverScale = 1.2f;
    public float hoverMoveY = 50f;

    private bool _isDragging = false;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        
        _rectTransform = GetComponent<RectTransform>();
        handCurveUI = GameObject.Find("HandCurveUI").GetComponent<HandCurveUI>();
    }
    
    // Hover
    public void OnPointerEnter(PointerEventData eventData)
    {
        _originalPosition = transform.localPosition;
        _originalScale = transform.localScale;
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
        transform.DOScale(_originalScale, 0.2f).SetEase(Ease.OutQuad);
        transform.DOLocalMove(_originalPosition, 0.2f).SetEase(Ease.OutCubic);

        if (_canvas != null)
        {
            _canvas.sortingOrder = _originalSortingOrder;
            _canvas.overrideSorting = false;
        }
    }

    // Drag
    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        GameManager.Instance.isDraggingCard = true;
        
        _originalPosition = _rectTransform.localPosition;
        _originalRotation = _rectTransform.localRotation;
        
        _originalParent = transform.parent;
        //transform.SetParent(transform.root, false); // UI 최상단으로 올림
        _canvasGroup.blocksRaycasts = false;     // Raycast 막기 → 드롭 감지 가능하게
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint = _rectTransform.localPosition;
        
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                Input.mousePosition,
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
                out localPoint))
        {
            _targetPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        GameManager.Instance.isDraggingCard = false;
        
        _canvasGroup.blocksRaycasts = true;
        //transform.SetParent(originalParent);

        // 드롭 실패 시 제자리 복귀
        // rectTransform.position = originalPosition;
        // rectTransform.rotation = originalRotation;
        
        _rectTransform.DOAnchorPos(_originalPosition, 0.3f).SetEase(Ease.OutExpo);
        _rectTransform.DOLocalRotateQuaternion(_originalRotation, 0.3f);
        
        // UI → 월드 좌표로 전환
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPoint.z = 0f;

        // 드롭 위치에 콜라이더가 있는지 확인
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            CardDrop cardDrop = hit.collider.GetComponent<CardDrop>();
            if (cardDrop != null)
            {
                Debug.Log("✅ DropZone 감지됨: 유닛 소환");
                DeckManager.Instance.UseCard(cardData);
                UnitSpawnManager.Instance.SpawnUnit(cardData, hit.point);
                handCurveUI.RefreshHandUI(DeckManager.Instance.hand);
            }
        }
    }

    private void Update()
    {
        _targetPosition = Input.mousePosition;
        
        if (_isDragging)
        {
            _rectTransform.position = Vector3.Lerp(_rectTransform.position, _targetPosition, Time.deltaTime * 20f);
        }
    }
}