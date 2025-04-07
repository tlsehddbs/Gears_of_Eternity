using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; 

public class UnitSpawner : MonoBehaviour
{
    
    public GameObject unitPrefab; // 소환할 유닛 프리팹
    public LayerMask canSpawnLayer; // 소환 포인트 레이어 마스크 

    public float minUnitSpacing = 1f; // 유닛 간 최소 거리
    public int maxCost = 10; // 유닛 최대 배치 가능 코스트
    private int currentCost = 0; // 현재 배치된 유닛의 코스트

    //public Text costText;

    private List<GameObject> spawnedUnits = new List<GameObject>();

    void Start()
    {
        //UpdateCostUI(); // 초기 UI 업데이트
    }

    
    void Update()
    {
        // 마우스 클릭 감지
        if(Input.GetMouseButtonDown(0))
        {
            // 마우스 클릭 위치에서 Ray 생성
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(ray, out RaycastHit hit, 100f, canSpawnLayer))
            {
                Vector3 spawnPos = hit.point;
                
                if(CanSpawnHere(spawnPos))
                {
                    TrySpawnUnit(spawnPos);  // 유닛 소환
                }
                else
                {
                    Debug.Log("소환할 수 없는 위치입니다.");
                }

            }
        }
    }

    bool CanSpawnHere(Vector3 pos)
    {
        foreach(GameObject unit in spawnedUnits)
        {
            if(unit == null) continue; // null 체크
            
            float dist = Vector3.Distance(unit.transform.position, pos);
            if(dist < minUnitSpacing)
            {
                return false; // 너무 가까운 유닛이 존재
            }   
        }
        return true; // 소환 가능   
    }

    /// <summary>
    /// 유닛을 해당 포인트에 소환하는 메서드
    /// </summary>
    void TrySpawnUnit(Vector3 position)
    {
        UnitCost costComponent = unitPrefab.GetComponent<UnitCost>();
        if(costComponent == null)
        {
            Debug.LogWarning("UnitPrefab에 UnitCost 컴포넌트가 없습니다.");
            return;
        }

        int unitCost = costComponent.cost;

        if(currentCost + unitCost > maxCost)
        {
            Debug.Log("소환 코스트가 부족합니다");
            return;
        }

        GameObject unit = Instantiate(unitPrefab, position, Quaternion.identity);
        spawnedUnits.Add(unit); // 소환된 유닛 리스트에 추가      

        currentCost += unitCost; // 현재 코스트 업데이트
        //UpdateCostUI(); // UI 업데이트}
    }

    /// <summary>
    /// UI 텍스트 갱신
    /// </summary>
    // void UpdateCostUI()
    // {
    //     if(costText != null)
    //     {
    //         costText.text = $"코스트: {currentCost}/{maxCost}";
    //     }
    // }
}