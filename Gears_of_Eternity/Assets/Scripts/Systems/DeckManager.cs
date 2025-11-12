using System.Collections.Generic;
using System.Linq;
using FactionTypes.Enums;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }
    
    [SerializeField]
    public CardCollection cardCollection;
    public CardUIHandler cardUIHandler;

    [Header("세력 필터링")] 
    public FactionType filterFaction = FactionType.IronGearFederation;
    
    public List<RuntimeUnitCard> deck = new List<RuntimeUnitCard>();
    public List<RuntimeUnitCard> hand = new List<RuntimeUnitCard>();
    public List<RuntimeUnitCard> usedCards = new List<RuntimeUnitCard>();

    
    // TODO: 게임 최초 실행이 아닌 이어하는 경우를 대비하여 게임 실행시 DeckManager Instance를 생성할 때 저장된 값에서 불러와 적용할 수 있도록 할 것.
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        cardCollection = GameObject.Find("CardCollection").GetComponent<CardCollection>();
        cardUIHandler = GameObject.Find("CardUIHandler").GetComponent<CardUIHandler>();
    }
    
    void Start()
    {
        // if (deck.Count == 0)
        // {
        //     InitializeDeck();
        //     //DrawCards(4);   // 초기 카드 draw (4장)
        // }
    }

    public void InitializeDeck()
    {
        Reset();
        
        // 덱 드로우를 위한 전체 유닛 카드 리스트화 및 세력별 필터링
        if (cardCollection.allAvailableCards == null || cardCollection.allAvailableCards.Count == 0)
        {
            Debug.LogWarning("카드 목록이 비어 있음");
        } 
        else 
        {
            // TODO: Unit 업그레이드를 고려하여 재설계 할 것 
            var filtered = cardCollection.allAvailableCards
                .Where(card => card.faction == filterFaction)
                .Where(card => card.level == 1)
                .ToList();
        
            if (filtered.Count == 0)
            {
                Debug.LogWarning($"⚠세력 '{filterFaction}'에 해당하는 카드가 없음");
                return;
            }
        
            // TODO: 근거리 5, 원거리 4, 지원 3 유닛을 지정 생성하도록 변경
            for (int i = 0; i < 12; i++)
            {
                var randomCard = filtered[Random.Range(0, filtered.Count)];
                var runtimeCardCopy = new RuntimeUnitCard(randomCard);
                
                deck.Add(runtimeCardCopy);
            }
            
        }
        Shuffle(deck);
    }

    /// <summary>
    /// Scene 시작 시 CardCollection과 CarUIHandler가 Null일 경우 다시 찾도록 함.
    /// Deck, Hand, UsedCard List를 초기화 함.
    /// </summary>
    void Reset()
    {
        if (cardCollection == null)
            cardCollection = FindObjectOfType<CardCollection>(includeInactive: true);

        if (cardUIHandler == null)
            cardUIHandler = FindObjectOfType<CardUIHandler>(includeInactive: true);
        
        deck.Clear();
        hand.Clear();
        usedCards.Clear();
    }

    /// <summary>
    /// card를 param의 값에 맞춰서 Draw 함.
    /// </summary>
    /// <param name="count">Draw할 Card의 수</param>
    public void DrawCards(int count)
    {
        // deck에 카드 부족 시 used에서 가져와 shuffle
        if (deck.Count < count)
        {
            deck.AddRange(usedCards);
            usedCards.Clear();
            Shuffle(deck);
        }

        //hand.Clear();
        
        for (int i = 0; i < count; i++)
        {
            if (deck.Count == 0) 
                break;
        
            var randindex = Random.Range(0, deck.Count);
            var card = deck[randindex];
            deck.RemoveAt(randindex);
            hand.Add(card);
        }

        // 카드 낱개 draw (임시 -> TODO: 카드 로테이션이 구체화 되는대로 수정할 예정)
        // if (count != 0)
        // {
        //     var randindex = Random.Range(0, deck.Count);
        //     var card = deck[randindex];
        //     deck.RemoveAt(randindex);
        //     hand.Add(card);
        // }
        
        cardUIHandler.AddCards(hand);
    }

    public void UseCard(RuntimeUnitCard card)
    {
        if (!hand.Contains(card))
        {
            //Debug.LogWarning($"[DeckManager] 핸드에 없는 카드 사용 시도: {card.unitName}");
            return;
        }

        hand.Remove(card);
        usedCards.Add(card);
    }

    // TODO: 협의한 내용에 따라서 소환 cost 보충 시기 등 고려할 것 
    // public void EndTurn()
    // {
    //     deck.AddRange(hand);
    //     hand.Clear();
    //
    //     Shuffle(deck);
    //     Debug.Log("턴 종료 → 덱 재구성 완료");
    // }

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
