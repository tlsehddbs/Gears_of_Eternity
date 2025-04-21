using System.Collections.Generic;
using System.Linq;
using FactionTypes.Enums;
using UnityEngine;
using UnityEngine.UI;

public class drawtest : MonoBehaviour
{
    public DeckManager deckManager;
    public CardCollection cardCollection;
    public Button testAddButton;
    
    [Header("세력 필터링")]
    public FactionType filterFaction = FactionType.IronGearFederation;
    
    void Start()
    {
        // 버튼 클릭 시 메서드 연결
        testAddButton.onClick.AddListener(AddRandomCardToDeck);
    }
    
    void AddRandomCardToDeck()
    {
        List<UnitCardData> allCards = cardCollection.allAvailableCards;

        if (allCards == null || allCards.Count == 0)
        {
            Debug.LogWarning("❗카드 목록이 비어 있음");
            return;
        }
        
        var filtered = allCards
            .Where(card => card.faction == filterFaction)
            .Where(card => card.level == 1)
            .ToList();

        if (filtered.Count == 0)
        {
            Debug.LogWarning($"⚠️세력 '{filterFaction}'에 해당하는 카드가 없음");
            return;
        }
        
        UnitCardData randomCard = filtered[Random.Range(0, filtered.Count)];
        bool success = deckManager.AddCard(randomCard);

        if (success)
        {
            Debug.Log($"덱에 카드 추가됨: {randomCard.unitName} / 세력 : {randomCard.faction}");
        }
        else
        {
            Debug.LogWarning($"⚠️카드 추가 실패: 덱이 가득 참");
        }
    }
}
