using System.Collections.Generic;
using System.Linq;
using FactionTypes.Enums;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }
    
    [SerializeField]
    public CardCollection cardCollection;
    public HandCurveUI handPanelManager;

    [Header("세력 필터링")] 
    public FactionType filterFaction = FactionType.IronGearFederation;
    
    public List<RuntimeUnitCard> deck = new List<RuntimeUnitCard>();
    public List<RuntimeUnitCard> hand = new List<RuntimeUnitCard>();
    public List<RuntimeUnitCard> usedCards = new List<RuntimeUnitCard>();

    
    // 중요 ! : DeckManager 가 게임 실행시 최초 씬에서 생성되게 하여야 함. 추후 Scene이 확장되고 난 이후 테스트를 진행해 볼 것.
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
    }
    
    void Start()
    {
        if (deck.Count == 0)
        {
            InitializeDeck();
            DrawCards(4);   // 초기 카드 draw
        }
    }

    void InitializeDeck()
    {
        // cardCollection Null Check
        if (cardCollection == null)
        {
            Debug.LogError("CardCollection 참조 없음");
            return;
        }
        
        //runtimeCards = cardCollection.allAvailableCards.Select(unitCard => new RuntimeUnitCard(unitCard)).ToList();
        
        // 덱 드로우를 위한 전체 유닛 카드 리스트화 및 세력별 필터링
        if (cardCollection.allAvailableCards == null || cardCollection.allAvailableCards.Count == 0)
        {
            Debug.LogWarning("❗카드 목록이 비어 있음");
        } 
        else 
        {
            var filtered = cardCollection.allAvailableCards
                .Where(card => card.faction == filterFaction)
                .Where(card => card.level == 1)
                .ToList();
        
            if (filtered.Count == 0)
            {
                Debug.LogWarning($"⚠️세력 '{filterFaction}'에 해당하는 카드가 없음");
                return;
            }
        
            for (int i = 0; i < 12; i++)
            {
                var randomCard = filtered[Random.Range(0, filtered.Count)];
                var runtimeCardCopy = new RuntimeUnitCard(randomCard);
                
                deck.Add(runtimeCardCopy);
            }
            
        }
        Shuffle(deck);
    }

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
        
        handPanelManager.RefreshHandUI(hand);
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
