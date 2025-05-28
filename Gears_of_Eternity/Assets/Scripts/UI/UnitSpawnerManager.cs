using UnityEngine;

public class UnitSpawnerManager : MonoBehaviour
{
    public static UnitSpawnerManager Instance;

    public GameObject unitPrefab;

    void Awake()
    {
        Instance = this;
    }

    public void SpawnUnit(RuntimeUnitCard card, Vector3 position)
    {
        var unit = Instantiate(unitPrefab, position, Quaternion.identity);
        //unit.GetComponent<UnitController>().Initialize(card);
        Debug.Log($"{card.unitName} 소환됨 at {position}");
    }
}
