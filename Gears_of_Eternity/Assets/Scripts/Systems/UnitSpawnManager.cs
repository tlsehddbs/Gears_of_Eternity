using UnityEngine;
using UnityEngine.Serialization;

public class UnitSpawnManager : MonoBehaviour
{
    public static UnitSpawnManager Instance { get; private set; }
    
    public GameObject defaultUnitPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    public void SpawnUnit(RuntimeUnitCard card, Vector3 position)
    {
        Instantiate(card.unitPrefab ? card.unitPrefab : defaultUnitPrefab, position, Quaternion.identity);
        
        Debug.Log($"{card.unitName} 소환됨 at {position}\n GUID : {card.uniqueId}");
    }
}
