using UnityEngine;
using UnityEngine.EventSystems;

public class CardDrop : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        CardUIManager droppedCard = eventData.pointerDrag?.GetComponent<CardUIManager>();

        if (droppedCard == null)
        {
            return;
        }

        Debug.Log($"드롭 성공! 카드: {droppedCard.cardData.unitName}");

        // 실제 유닛 소환
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0;

        UnitSpawnManager.Instance.SpawnUnit(droppedCard.cardData, worldPos);

        // 핸드에서 제거
        DeckManager.Instance.UseCard(droppedCard.cardData);
        Destroy(droppedCard.gameObject);
    }
}