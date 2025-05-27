using System.Collections.Generic;
using System.Linq;
using FactionTypes.Enums;
using Unity.VisualScripting;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }
    
    [SerializeField]
    public CardCollection cardCollection;

    [Header("세력 필터링")] 
    public FactionType filterFaction = FactionType.IronGearFederation;
    
    //public List<UnitCardData> cards;
    public List<RuntimeUnitCard> runtimeCards;
    public List<RuntimeUnitCard> deck = new List<RuntimeUnitCard>();
    public List<RuntimeUnitCard> hand = new List<RuntimeUnitCard>();
    public List<RuntimeUnitCard> usedCards = new List<RuntimeUnitCard>();

    public int drawCount = 4;
    
    // 중요 ! : DeckManager 가 게임 실행시 최초 씬에서 생성되게 하여야 함. 추후 Scene이 확장되고 난 이후 테스트를 진행해 볼 것.
    // TODO : 게임 최초 실행이 아닌 이어하는 경우를 대비하여 게임 실행시 DeckManager Instance를 생성할 때 저장된 값에서 불러와 적용할 수 있도록 할 것.
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
            DrawCards(0);
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
        
        runtimeCards = cardCollection.allAvailableCards.Select(unitCard => new RuntimeUnitCard(unitCard)).ToList();
        
        // 덱 드로우를 위한 전체 유닛 카드 리스트화 및 세력별 필터링
        if (runtimeCards == null || runtimeCards.Count == 0)
        {
            Debug.LogWarning("❗카드 목록이 비어 있음");
        }
        
        var filtered = runtimeCards
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
            RuntimeUnitCard randomCard = filtered[Random.Range(0, filtered.Count)];
            deck.Add(randomCard);
            
            Debug.Log($"덱에 카드 추가됨: {randomCard.unitName} / 세력 : {randomCard.faction} / 레벨 : {randomCard.level}");
        }

        Shuffle(deck);
    }

    public void DrawCards(int count)
    {
        // 덱이 부족하면 사용된 카드를 덱에 합친 후 셔플
        // if (deck.Count < drawCount)
        // {
        //     Debug.Log("덱 부족 → 사용한 카드 합쳐서 다시 덱 구성");
        //     deck.AddRange(usedCards);
        //     usedCards.Clear();
        //     Shuffle(deck);
        // }

        //hand.Clear();

        // for (int i = 0; i < count; i++)
        // {
        //     if (deck.Count == 0) break;
        //
        //     var card = deck[0];
        //     deck.RemoveAt(0);
        //     hand.Add(card);
        // }

        if (deck.Count == 0)
        {
            if (usedCards.Count == 0)
            {
                Debug.LogWarning("재활용할 카드 없음");
                return;
            }
            Debug.Log("덱 부족 -> used 카드 재활용 셔플");
            deck.AddRange(usedCards);
            usedCards.Clear();
            Shuffle(deck);
        }

        if (count != 0)
        {
            var card = deck[0];
            deck.RemoveAt(0);
            hand.Add(card);
        }
        Debug.Log("드로우 완료. 현재 핸드: " + hand.Count);
    }

    public void UseCard(/*RuntimeUnitCard card*/int index)
    {
        //if (hand.Contains(card))
        //{
        //    hand.Remove(card);
        //    usedCards.Add(card);
        //
        //    Debug.Log(card.unitName + " 사용됨");
        //}
        
        if (index < 0 || index >= hand.Count)
        {
            Debug.LogWarning($"[DeckManager] 유효하지 않은 인덱스 접근: {index}");
            return;
        }

        RuntimeUnitCard selectedCard = hand[index];

        // 실제 카드 사용 로직 추가 가능: ex. selectedCard.ActivateSkill() 등

        // 카드 이동
        usedCards.Add(selectedCard);
        hand.RemoveAt(index);

        Debug.Log($"[DeckManager] 카드 사용됨: {selectedCard.unitName}, used로 이동");
    }

    // TODO : 협의한 내용에 따라서 소환 cost 보충 시기 등 고려할 것 
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
