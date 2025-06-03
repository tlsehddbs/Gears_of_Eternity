using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    
    private RectTransform rectTransform;
    private Transform originalParent;

    public RuntimeUnitCard cardData;

    public HandCurveUI handCurveUI;
    
    private Vector3 targetPosition;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private bool isDragging = false;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        
        rectTransform = GetComponent<RectTransform>();
        handCurveUI = GameObject.Find("HandCurveUI").GetComponent<HandCurveUI>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        GameManager.Instance.isDraggingCard = true;
        
        originalPosition = rectTransform.localPosition;
        originalRotation = rectTransform.localRotation;
        
        originalParent = transform.parent;
        //transform.SetParent(transform.root, false); // UI 최상단으로 올림
        canvasGroup.blocksRaycasts = false;     // Raycast 막기 → 드롭 감지 가능하게
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint = rectTransform.localPosition;
        
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                Input.mousePosition,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out localPoint))
        {
            targetPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        GameManager.Instance.isDraggingCard = false;
        
        canvasGroup.blocksRaycasts = true;
        //transform.SetParent(originalParent);

        // 드롭 실패 시 제자리 복귀
        // rectTransform.position = originalPosition;
        // rectTransform.rotation = originalRotation;
        
        rectTransform.DOAnchorPos(originalPosition, 0.3f).SetEase(Ease.OutExpo);
        rectTransform.DOLocalRotateQuaternion(originalRotation, 0.3f);
        
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
                return;
            }
        }
    }

    private void Update()
    {
        targetPosition = Input.mousePosition;
        
        if (isDragging)
        {
            rectTransform.position = Vector3.Lerp(rectTransform.position, targetPosition, Time.deltaTime * 20f);
        }
    }
}