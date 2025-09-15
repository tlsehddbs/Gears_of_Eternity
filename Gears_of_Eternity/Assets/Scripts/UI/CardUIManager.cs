using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardUIManager : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    private CanvasGroup _canvasGroup;
    private Canvas _canvas;
    private RectTransform _rectTransform;
    //private Transform _originalParent;

    public RuntimeUnitCard cardData;
    public CardUIHandler cardUIHandler;

    private Tween currentTween;

    // 배치 실패시 원래의 Position으로 복귀를 위한 변수
    private Vector3 originalPosition;
    private Quaternion _originalRotation;
    
    private Vector3 originalScale;

    public float hoverScale = 1.2f;
    public float hoverMoveY = 50f;

    private bool _isCardHighlighted;
    
    // TODO: 카드가 하이라이팅이 되었는지 확인하는 boolean 변수를 만들고 하이라이팅이 되지 않은 상태에서 카드를 클릭했을 경우 등, 여러 상황에서도 동일하게 작동하는 것을 보장하기 위해 세부적으로 구현할 것
    
    
    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        
        _rectTransform = GetComponent<RectTransform>();
        cardUIHandler = GameObject.Find("CardUIHandler").GetComponent<CardUIHandler>();
    }

    
    public void UpdateOriginalTransform(Vector2 position)
    {
        originalPosition = position;
        originalScale = new Vector3(1, 1, 1);  // 고정값
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

        currentTween?.Kill();
        currentTween = DOTween.Sequence()
            .Join(transform.DOScale(hoverScale, 0.2f).SetEase(Ease.OutBack))
            .Join(transform.DOLocalMoveY(originalPosition.y + (!onlyScaleHighlightEffect ? hoverMoveY : 0), 0.2f).SetEase(Ease.OutCubic))
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
        
        currentTween?.Kill();

        if (instantKillHighlightAnim)
        {
            transform.localScale = originalScale;
        }
        else
        {
            currentTween = DOTween.Sequence()
                .Join(transform.DOScale(new Vector3(1, 1, 1), 0.2f).SetEase(Ease.OutQuad))
                .Join(transform.DOLocalMove(originalPosition, 0.2f).SetEase(Ease.OutCubic))
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
        
        currentTween?.Kill();
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
        
        // UI를 월드 좌표로 전환
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
                
                cardUIHandler.RemoveCards(DeckManager.Instance.hand);
            }
        }
    }
}