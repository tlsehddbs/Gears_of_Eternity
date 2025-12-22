using System.Collections.Generic;
using FactionTypes.Enums;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }
    
    [SerializeField] public CardCollection cardCollection;
    [SerializeField] public CardUIHandler cardUIHandler;

    [Header("세력 필터링")] 
    public FactionType filterFaction = FactionType.IronGearFederation;
    
    public List<RuntimeUnitCard> deck = new List<RuntimeUnitCard>();
    public List<RuntimeUnitCard> hand = new List<RuntimeUnitCard>();
    public List<RuntimeUnitCard> usedCards = new List<RuntimeUnitCard>();

    
    // TODO: 게임 최초 실행이 아닌 이어하는 경우를 대비하여 게임 실행시 DeckManager Instance를 생성할 때 저장된 값에서 불러와 적용할 수 있도록 할 것.
    private void Awake()
    {
        Instance = this;
        
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
        BuildDeckFromPlayerState(PlayerState.Instance);
    }
    
    public void BuildDeckFromPlayerState(PlayerState state)
    {
        ResetCombatDeck();

        if (state == null)
        {
            Debug.LogError("[DeckManager] PlayerState is null");
            return;
        }
        
        foreach (var def in state.DeckCards)
        {
            if (def == null)
            {
                continue;
            }
            deck.Add(def);     // 여기서 uniqueId 생성
        }
        Shuffle(deck);
    }

    public void ResetCombatDeck()
    {
        deck.Clear();
        hand.Clear();
        usedCards.Clear();
    }

    public void DrawCards(int count)
    {
        // TODO: 카드 로테이션을 구체화 한 다음 관련 로직을 적용할 것
        // deck에 카드 부족 시 used에서 가져와 shuffle
        if (deck.Count < count)
        {
            deck.AddRange(usedCards);
            usedCards.Clear();
            Shuffle(deck);
        }
        
        for (int i = 0; i < count; i++)
        {
            if (deck.Count == 0)
            {
                break;
            }
            
            var randomIndex = Random.Range(0, deck.Count);
            var card = deck[randomIndex];
            deck.RemoveAt(randomIndex);
            hand.Add(card);
        }
        
        cardUIHandler.AddCards(hand);
    }

    public void UseCard(RuntimeUnitCard card)
    {
        if (card == null)
        {
            return;
        }
        
        if (!hand.Remove(card))
        {
            return;
        }

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
        if (list == null || list.Count <= 1)
        {
            return;
        }
        
        // Fisher-Yates
        for (int i = 0; i < list.Count - 1; i++)
        {
            // var temp = list[i];
            // int randomIndex = Random.Range(i, list.Count);
            // list[i] = list[randomIndex];
            // list[randomIndex] = temp;
            
            int randomIndex = Random.Range(0, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }
}
