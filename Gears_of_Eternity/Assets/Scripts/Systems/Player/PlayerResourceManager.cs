using System.Collections.Generic;
using UnityEngine;

public class PlayerResourceManager : MonoBehaviour, IGetPlayerProgress
{
    public static PlayerResourceManager Instance { get; private set; }
    
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

    [SerializeField] private Dictionary<string, int> _itemCounts = new Dictionary<string, int>();

    public int GetItemCount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return 0;

        if (_itemCounts.TryGetValue(itemId, out int count))
            return count;

        return 0;
    }
}

public interface IGetPlayerProgress
{
    /// <summary> 해당 아이템이 몇 개 있는지 반환 </summary>
    int GetItemCount(string itemId);
}