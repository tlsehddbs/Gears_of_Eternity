using UnityEngine;
using UnityEngine.EventSystems;

public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Transform originalParent;

    public RuntimeUnitCard cardData;

    public HandCurveUI handCurveUI;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        handCurveUI = GameObject.Find("HandCurveUI").GetComponent<HandCurveUI>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        transform.SetParent(transform.root); // UI 최상단으로 올림
        canvasGroup.blocksRaycasts = false; // Raycast 막기 → 드롭 감지 가능하게
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        transform.SetParent(originalParent);

        // 드롭 실패 시 제자리 복귀
        rectTransform.anchoredPosition = Vector2.zero;
        
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

        Debug.Log("⛔ DropZone이 아님: 카드 복귀");
    }
}