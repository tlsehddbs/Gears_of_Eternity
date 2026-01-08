using UnityEngine;
using UnityEngine.EventSystems;

public class CardDrop : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        CardUIHandler droppedCard = eventData.pointerDrag?.GetComponent<CardUIHandler>();

        if (droppedCard == null)
        {
            return;
        }

        var data = droppedCard.GetComponent<RuntimeUnitCardRef>().Card;
        Debug.Log($"드롭 성공! 카드: {data.unitName}");

        // 실제 유닛 소환
        if (Camera.main != null)
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            worldPos.z = 0;

            UnitSpawnManager.Instance.SpawnUnit(data, worldPos);
        }

        // 핸드에서 제거
        DeckManager.Instance.UseCard(data);
        Destroy(droppedCard.gameObject);
    }
}