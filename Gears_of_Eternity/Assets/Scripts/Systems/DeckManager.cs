using System.Collections.Generic;
using System.Linq;
using FactionTypes.Enums;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [SerializeField]
    public CardCollection cardCollection;

    [Header("세력 필터링")] 
    public FactionType filterFaction = FactionType.IronGearFederation;
    
    public List<UnitCardData> cards;
    public List<RuntimeUnitCard> deck = new List<RuntimeUnitCard>();
    public List<RuntimeUnitCard> hand = new List<RuntimeUnitCard>();
    public List<RuntimeUnitCard> usedCards = new List<RuntimeUnitCard>();

    public int drawCount = 4;
    

    void Start()
    {
        InitializeDeck();
        DrawCards();
    }

    void InitializeDeck()
    {
        // cardCollection Null Check
        if (cardCollection == null)
        {
            Debug.LogError("CardCollection 참조 없음");
            return;
        }
        
        // cardCollection의 list말고 플레이어가 플레이 중 값을 조작할 가능성이 있는 유닛 데이터는 별도로 복사하여 list로 사용할 것
        cards = new List<UnitCardData>(cardCollection.allAvailableCards);

        
        // 덱 드로우를 위한 전체 유닛 카드 리스트화 및 세력별 필터링
        if (cards == null || cards.Count == 0)
        {
            Debug.LogWarning("❗카드 목록이 비어 있음");
        }
        
        var filtered = cards
            .Where(card => card.faction == filterFaction)
            .Where(card => card.level == 1)
            .ToList();
        
        if (filtered.Count == 0)
        {
            Debug.LogWarning($"⚠️세력 '{filterFaction}'에 해당하는 카드가 없음");
            return;
        }
        
        // 테스트용 초기 덱 세팅 (12장)
        for (int i = 0; i < 12; i++)
        {
            UnitCardData randomCard = filtered[Random.Range(0, filtered.Count)];
            deck.Add(new RuntimeUnitCard(randomCard));
            
            Debug.Log($"덱에 카드 추가됨: {randomCard.unitName} / 세력 : {randomCard.faction} / 레벨 : {randomCard.level}");
        }

        Shuffle(deck);
    }

    public void DrawCards()
    {
        // 덱이 부족하면 사용된 카드를 덱에 합친 후 셔플
        if (deck.Count < drawCount)
        {
            Debug.Log("덱 부족 → 사용한 카드 합쳐서 다시 덱 구성");
            deck.AddRange(usedCards);
            usedCards.Clear();
            Shuffle(deck);
        }

        hand.Clear();

        for (int i = 0; i < drawCount; i++)
        {
            if (deck.Count == 0) break;

            var card = deck[0];
            deck.RemoveAt(0);
            hand.Add(card);
        }

        Debug.Log("드로우 완료. 현재 핸드: " + hand.Count);
    }

    public void UseCard(RuntimeUnitCard card)
    {
        if (hand.Contains(card))
        {
            hand.Remove(card);
            usedCards.Add(card);

            Debug.Log(card.unitName + " 사용됨");
        }
    }

    // TODO : 협의한 내용에 따라서 소환 cost 보충 시기 등을 고려하여 덱 로테이션을 어떻게 할 것인지 고민하고 구현할 것
    public void EndTurn()
    {
        // 남은 핸드는 다시 덱으로 보냄
        deck.AddRange(hand);
        hand.Clear();

        Shuffle(deck);
        Debug.Log("턴 종료 → 덱 재구성 완료");
    }

    void Shuffle(List<RuntimeUnitCard> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}
