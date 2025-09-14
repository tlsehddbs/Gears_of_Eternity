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
    private Vector3 originalPosition;
    private Quaternion _originalRotation;
    
    private Vector3 originalScale;
    private int _originalSortingOrder;

    public float hoverScale = 1.2f;
    public float hoverMoveY = 50f;
    
    
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

        if (!GameManager.Instance.isPointerEventEnabled || GameManager.Instance.isDraggingCard)
        {
            return;
        }
        
        currentTween?.Kill();
        currentTween = DOTween.Sequence()
            .Join(transform.DOScale(hoverScale, 0.2f).SetEase(Ease.OutBack))
            .Join(transform.DOLocalMoveY(originalPosition.y + hoverMoveY, 0.2f).SetEase(Ease.OutCubic))
            .SetUpdate(false)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (GameManager.Instance.isDraggingCard)
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
            //transform.localPosition = originalPosition;
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

    
    // Drag
    public void OnBeginDrag(PointerEventData eventData)
    {
        // TODO: 드래그 시 부모를 해제하고 다른 카드 밑으로 들어가지 않게 가장 위에 표시가 되게끔 변경
        
        GameManager.Instance.isDraggingCard = true;
        // GameManager.Instance.isInteractable = false;
        
        //_originalParent = transform.parent;
        //transform.SetParent(_canvas.transform);   // UI 최상단으로 올림
        _canvasGroup.blocksRaycasts = false;        // Raycast 차단, 드롭 감지 가능하게
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
#if UNITY_EDITOR
        Debug.Log($"mouse position : {Input.mousePosition}   /   {_rectTransform.position} : rect position");
#endif
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        GameManager.Instance.isDraggingCard = false;
        // GameManager.Instance.isInteractable = true;
        
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
                
                cardUIHandler.RemoveCards(DeckManager.Instance.hand);
            }
            //cardUIHandler.RefreshHandUI(DeckManager.Instance.hand);
            //cardUIHandler.UpdateCardLayout();
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.X))
        {
            OnPointerExitAnimation(true);
        }
#endif
    }
}