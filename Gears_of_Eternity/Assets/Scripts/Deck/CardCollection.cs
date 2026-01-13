using System.Collections.Generic;
using System.Linq;
using UnitRoleTypes.Enums;
using UnityEngine;

public class CardCollection : MonoBehaviour
{
    public static CardCollection Instance { get; private set; }
    
    public List<UnitCardData> allAvailableCards = new List<UnitCardData>();

    private Dictionary<string, UnitCardData> _cardById;
    
    
    private void Awake()
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
        
        LoadCardsFromResources();
        BuildCache();
    }

    // 유닛 업그레이드 기능을 어떻게 구현할지, 모든 유닛을 가져올 필요가 있는지, 그렇지 않다면 런타임 유닛의 정보를 어떻게 수정하여 리소스를 줄일 수 있을지 고민해봐야 할 듯 
    void LoadCardsFromResources()
    {
        allAvailableCards = Resources.LoadAll<UnitCardData>("UnitCardAssets").ToList();
    }

    void BuildCache()
    {
        _cardById = new Dictionary<string, UnitCardData>();
        foreach (var card in allAvailableCards)
        {
            if (!string.IsNullOrEmpty(card.id))
            {
                _cardById[card.id] = card;
            }
        }
    }

    public UnitCardData GetById(string cardId)
    {
        if (string.IsNullOrEmpty(cardId))
        {
            return null;
        }
        return _cardById.TryGetValue(cardId, out var card) ? card : null;
    }
    
    // 타입별 필터
    public List<UnitCardData> GetByRole(RoleTypes role)
    {
        return allAvailableCards.Where(c => c.roleType == role).ToList();
    }
}
