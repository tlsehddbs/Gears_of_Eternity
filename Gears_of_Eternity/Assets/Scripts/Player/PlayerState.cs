using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using FactionTypes.Enums;
using UnitRoleTypes.Enums;
using Random = UnityEngine.Random;

public class PlayerState : MonoBehaviour, IPlayerProgress
{
    public static PlayerState Instance { get; private set; }
    
    // Life
    
    // Gold
    [Header("Gold")] 
    [SerializeField] private int gold = 50;
    public int Gold => gold;

    public event Action<int> OnGoldChanged;

    [Header("Run Economy")] 
    [SerializeField] private int upgradeCount = 0;
    public int UpgradeCount => upgradeCount;
    
    
    // ========== Inventory ===========
    [Header("Inventory (Serialized)")] 
    [SerializeField] private List<ItemStack> inventory = new List<ItemStack>();
    
    // 런타임 캐시
    private readonly Dictionary<string, int> _inventory = new Dictionary<string, int>();

    public event Action<string, int> OnItemCountChanged;   // (itemId, isActive)
    
    
    // ========== Active Item ===========
    [Header("Active Items (Serialized)")]
    [SerializeField] private List<string> activeItemIds = new List<string>();
    
    // 런타임 캐시
    private readonly HashSet<string> _activeItems = new HashSet<string>();

    public event Action<string, bool> OnActiveItemChanged;    // (itemId, isActive)
    
    
    // ========== Deck ===========
    [Header("Deck (Cards)")]
    [Tooltip("플레이어 덱 구성. 동일한 UnitCardData를 여러 장 넣을 수 있음. 런타임 고유 ID는 RuntimeUnitCard가 생성 시 부여함.")]
    [SerializeField] private List<RuntimeUnitCard> deckCards = new List<RuntimeUnitCard>();
    
    public List<RuntimeUnitCard> DeckCards => deckCards;
    
    public event Action OnDeckChanged;
    
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            RefreshCaches();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RefreshCaches()
    {
        _inventory.Clear();
        foreach (var s in inventory)
        {
            if (string.IsNullOrEmpty(s.itemId) || s.count <= 0)
            {
                continue;
            }
            _inventory[s.itemId] = s.count;
        }

        _activeItems.Clear();
        foreach (var id in activeItemIds)
        {
            if (!string.IsNullOrEmpty(id))
            {
                _activeItems.Add(id);
            }
        }
    }
    

    // ---------- IPlayerProgress ----------
    public int GetItemCount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return 0;
        }
        return _inventory.TryGetValue(itemId, out var count) ? count : 0;
    }

    
    // ---------- Gold ----------
    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        gold += amount;
        OnGoldChanged?.Invoke(gold);
    }

    public bool TrySpendGold(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (gold < amount)
        {
            return false;
        }
        
        gold -= amount;
        OnGoldChanged?.Invoke(gold);
        
        return true;
    }

    // public bool TryPurchase(string targetUniqueId, RuntimeUnitCard card, int goldAmount)
    // {
    //     if (string.IsNullOrEmpty(targetUniqueId) || card == null)
    //     {
    //         return false;
    //     }
    //
    //     if (!TrySpendGold(goldAmount))
    //     {
    //         return false;
    //     }
    //
    //     return true;
    // }

    public void ResetRunEconomy()
    {
        upgradeCount = 0;
    }

    public void IncrementUpgradeCount()
    {
        upgradeCount++;
    }

    public void ResetUpgradeCount()
    {
        upgradeCount = 0;
    }
    
    // ---------- Inventory ----------
    public void AddItem(string itemId, int amount = 1)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
        {
            return;
        }

        if (_inventory.ContainsKey(itemId))
        {
            _inventory[itemId] += amount;
        }
        else
        {
            _inventory[itemId] = amount;
        }

        SyncInventoryListFromCache();
        OnItemCountChanged?.Invoke(itemId, _inventory[itemId]);
    }

    public bool RemoveItem(string itemId, int amount = 1)
    {
        if (!_inventory.TryGetValue(itemId, out var cur) || cur < amount)
        {
            return false;
        }
        
        cur -= amount;
        if (cur == 0)
        {
            _inventory.Remove(itemId);
        }
        else
        {
            _inventory[itemId] = cur;
        }

        SyncInventoryListFromCache();
        OnItemCountChanged?.Invoke(itemId, GetItemCount(itemId));
        return true;
    }
    
    private void SyncInventoryListFromCache()
    {
        inventory.Clear();
        foreach (var kv in _inventory)
        {
            inventory.Add(new ItemStack { itemId = kv.Key, count = kv.Value });
        }
    }
    
    
    // ---------- Active Item ----------
    public bool IsActiveItem(string itemId) => !string.IsNullOrEmpty(itemId) && _activeItems.Contains(itemId);

    public void ActivateItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return;
        }
        
        if (_activeItems.Add(itemId))
        {
            SyncActiveListFromCache();
            OnActiveItemChanged?.Invoke(itemId, true);
        }
    }

    public void DeactivateItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return;
        }
        
        if (_activeItems.Remove(itemId))
        {
            SyncActiveListFromCache();
            OnActiveItemChanged?.Invoke(itemId, false);
        }
    }

    private void SyncActiveListFromCache()
    {
        activeItemIds.Clear();
        activeItemIds.AddRange(_activeItems);
    }
    
    
    // ---------- Deck ----------
    public void SetDeck(IEnumerable<RuntimeUnitCard> cards)
    {
        deckCards.Clear();
        if(cards != null)
        {
            foreach (var c in cards)
            {
                if (c != null)
                {
                    deckCards.Add(c);
                }
            }
        }
        OnDeckChanged?.Invoke();
    }

    /// <summary>
    /// 기본 덱 생성: 역할 비율에 맞춰 UnitCardData(정의) 레퍼런스를 여러 장 넣어둠.
    /// RuntimeUnitCard의 uniqueId는 전투 진입 시 생성됨
    /// </summary>
    public void GenerateStarterDeck(CardCollection collection)
    {
        if (collection == null)
        {
            Debug.LogError("[PlayerState] CardCollection is null");
            return;
        }
        
        deckCards.Clear();
        
        // 유닛 역할 별 비율 설정
        int meleeCount = 5;
        int rangedCount = 4;
        int supportCount = 3;
        
        AddRandomCards(collection, RoleTypes.Melee, meleeCount);
        AddRandomCards(collection, RoleTypes.Ranged, rangedCount);
        AddRandomCards(collection, RoleTypes.Support, supportCount);
        
        // Fisher-Yates
        for (int i = 0; i < deckCards.Count - 1; i++)
        {
            int randomIndex = Random.Range(0, deckCards.Count);
            (deckCards[i], deckCards[randomIndex]) = (deckCards[randomIndex], deckCards[i]);
        }
        
        OnDeckChanged?.Invoke();
    }

    private void AddRandomCards(CardCollection collection, RoleTypes role, int count)
    {
        var pool = collection.GetByRole(role);
        if (pool == null || pool.Count == 0)
        {
            Debug.LogWarning($"[PlayerState] No cards for role {role}");
            return;
        }
        
        // TODO: 임시로 하나의 세력으로 고정함(나중에 세력을 선택 할 수 있게하는 등의 기능을 추가할 예정이라면 별도의 설정을 할 수 있는 창 도는 메인메뉴에서 설정이 가능하도록 수정할 것
        var filteredFaction = pool
            .Where(fc => fc.faction == FactionType.IronGearFederation)
            .Where(fc => fc.level == 1)
            .ToList();
        
        if (filteredFaction.Count == 0)
        {
            Debug.LogWarning($"[PlayerState] IronGearFederation 에 해당하는 카드가 없음");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            var card = filteredFaction[Random.Range(0, filteredFaction.Count)];
            var runtimeCardCopy = new RuntimeUnitCard(card);
            
            if (card != null)
            {
                deckCards.Add(runtimeCardCopy);
            }
        }
    }

    public bool TryGetRandomUpgradeableCard(out RuntimeUnitCard pickedCard)
    {
        List<RuntimeUnitCard> candidates = new();
        
        foreach (var c in DeckCards)
        {
            if (c == null)
            {
                continue;
            }
            
            if (c.level >= 3)
            {
                continue;
            }

            var upgrades = c.nextUpgradeUnits;
            if (upgrades == null || upgrades.Count == 0)
            {
                continue;
            }
            candidates.Add(c);
        }

        if (candidates.Count == 0)
        {
            pickedCard = null;
            return false;
        }

        pickedCard = candidates[Random.Range(0, candidates.Count)];
        return true;
    }

    public bool TryOverwriteDeckCardKeepingUniqueId(string uniqueId, RuntimeUnitCard upgradeOptionRuntime)
    {
        if (string.IsNullOrEmpty(uniqueId) || upgradeOptionRuntime == null)
        {
            return false;
        }
        
        // deckCard 실제 원소를 찾아서 그 객체를 덮어 씀
        RuntimeUnitCard cur = null;
        for (int i = 0; i < deckCards.Count; i++)
        {
            var c = deckCards[i];
            if (c == null)
            {
                continue;
            }

            if (c.uniqueId == uniqueId)
            {
                cur = c;
                break;
            }
        }

        if (cur == null)
        {
            Debug.LogWarning("[TryOverwriteDeckCardKeepingUniqueId] 업그레이드 대상 유닛을 찾지 못함");
            return false;
        }
        
        // 오염 방지
        // - upgradeOptionRuntime은 건드리지 않음
        // - uniqueId는 cur의 것을 유지
        string keepUniqueId = cur.uniqueId;
        
        cur.uniqueId        = keepUniqueId;
        cur.unitName        = upgradeOptionRuntime.unitName;
        cur.unitDescription = upgradeOptionRuntime.unitDescription;
        cur.roleType        = upgradeOptionRuntime.roleType;
        cur.unitPrefab      = upgradeOptionRuntime.unitPrefab;
        cur.health          = upgradeOptionRuntime.health;
        cur.defense         = upgradeOptionRuntime.defense;
        cur.moveSpeed       = upgradeOptionRuntime.moveSpeed;
        cur.attackType      = upgradeOptionRuntime.attackType;
        cur.attackValue     = upgradeOptionRuntime.attackValue;
        cur.attackSpeed     = upgradeOptionRuntime.attackSpeed;
        cur.attackDistance  = upgradeOptionRuntime.attackDistance;
        cur.level           = upgradeOptionRuntime.level;
        cur.rarity          = upgradeOptionRuntime.rarity;
        cur.cost            = upgradeOptionRuntime.cost;
        

        if (cur.nextUpgradeUnits == null)
        {
            cur.nextUpgradeUnits = new List<UnitCardData>();
        }
        else
        {
            cur.nextUpgradeUnits.Clear();
        }
        
        if (upgradeOptionRuntime.nextUpgradeUnits != null)
        {
            cur.nextUpgradeUnits.AddRange(upgradeOptionRuntime.nextUpgradeUnits);
        }
        
        OnDeckChanged?.Invoke();
        return true;
    }
}

[Serializable]
public struct ItemStack
{
    public string itemId;
    public int count;
}

public interface IPlayerProgress
{
    int GetItemCount(string itemId);
}
