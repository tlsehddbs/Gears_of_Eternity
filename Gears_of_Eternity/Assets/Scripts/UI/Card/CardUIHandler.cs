using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardUIHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    private CanvasGroup _canvasGroup;
    private Canvas _canvas;
    private RectTransform _rectTransform;

    private RuntimeUnitCardRef _ref;
    private CardUIManager _cardUIManager;

    private Tween _currentTween;

    // 배치 실패시 원래의 Position으로의 복귀를 위한 변수
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    
    private Vector3 _originalScale;

    public float hoverScale = 1.2f;
    public float hoverMoveY = 50f;

    private bool _isCardHighlighted;
    
    
    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        
        _rectTransform = GetComponent<RectTransform>();
        _cardUIManager = GameObject.Find("CardUIManager").GetComponent<CardUIManager>();
        
        _ref = GetComponent<RuntimeUnitCardRef>();

    }

    
    public void UpdateOriginalTransform(Vector2 position)
    {
        _originalPosition = position;
        _originalScale = new Vector3(1, 1, 1);  // 고정값
    }
    
    
    // Hover
    public void OnPointerEnter(PointerEventData eventData)
    {
        OnPointerEnterEffect(false);
    }

    private void OnPointerEnterEffect(bool onlyScaleHighlightEffect)
    {
        if (!GameManager.Instance.isPointerEventEnabled || GameManager.Instance.isDraggingCard)
        {
            return;
        }
        
        _isCardHighlighted = true;

        _currentTween?.Kill();
        _currentTween = DOTween.Sequence()
            .Join(transform.DOScale(hoverScale, 0.2f).SetEase(Ease.OutBack))
            .Join(transform.DOLocalMoveY(_originalPosition.y + (!onlyScaleHighlightEffect ? hoverMoveY : 0), 0.2f).SetEase(Ease.OutCubic))
            .SetUpdate(false)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (GameManager.Instance.isDraggingCard)
        {
            return;
        }
        
        OnPointerExitEffect(false);
    }

    public void OnPointerExitEffect(bool instantKillHighlightAnim)
    {
        _isCardHighlighted = false;
        
        _currentTween?.Kill();

        if (instantKillHighlightAnim)
        {
            transform.localScale = _originalScale;
        }
        else
        {
            _currentTween = DOTween.Sequence()
                .Join(transform.DOScale(new Vector3(1, 1, 1), 0.2f).SetEase(Ease.OutQuad))
                .Join(transform.DOLocalMove(_originalPosition, 0.2f).SetEase(Ease.OutCubic))
                .SetUpdate(false)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        if (!GameManager.Instance.isPointerEventEnabled)
        {
            return;
        }
        
        if (!_isCardHighlighted)
        {
            OnPointerEnterEffect(true);
        }
    }
    
    // Drag
    public void OnBeginDrag(PointerEventData eventData)
    {
        // TODO: 드래그 시 부모를 해제하고 다른 카드 밑으로 들어가지 않게 가장 위에 표시가 되게끔 변경
        GameManager.Instance.isDraggingCard = true;
        
        _currentTween?.Kill();
        _canvasGroup.blocksRaycasts = false;        // Raycast 차단, 드롭 감지 가능하게
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        GameManager.Instance.isDraggingCard = false;
        _canvasGroup.blocksRaycasts = true;

        // 드롭 위치에 콜라이더가 있는지 확인
        if (Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                CardDrop cardDrop = hit.collider.GetComponent<CardDrop>();
            
                if (cardDrop != null)
                {
                    DeckManager.Instance.UseCard(_ref.Card);
                    UnitSpawnManager.Instance.SpawnUnit(_ref.Card, hit.point);
                
                    _cardUIManager.RemoveCards(DeckManager.Instance.hand);
                }
                else
                {
#if UNITY_EDITOR
                    Debug.LogWarning("배치 실패");
#endif
                    _cardUIManager.UpdateCardLayout();
                }
            }
        }
    }
}