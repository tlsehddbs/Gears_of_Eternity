using UnityEngine;

public class UnitSpawnManager : MonoBehaviour
{
    public static UnitSpawnManager Instance { get; private set; }

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
        // TODO: hand list에서 맵에 배치, 스폰을 했을 때 guid를 포함한 카드에 있는 유닛 데이터를 유닛 프리팹에 넣을 수 있는 방법을 마련할 것
        
        var unit = Instantiate(card.unitPrefab, position, Quaternion.identity);
        Debug.Log($"{card.unitName} 소환됨 at {position}");
        
        // TODO: guid 를 기반으로 하여 덱 로테이션에 지장이 없게, 유효한 유닛은 used list에서 deck list로 로테이션 방지할 수 있는 대책이 필요 
    }
}
