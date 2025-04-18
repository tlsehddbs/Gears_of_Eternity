using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEditor.PackageManager;


public class UnitSpawner : MonoBehaviour
{
    
    public GameObject unitPrefab;       // 소환할 유닛 프리팹
    public LayerMask canSpawnLayer;     // 소환 포인트 레이어 마스크 
    public LayerMask unitLayer;         //배치된 유닛을 탐지하는 레이어

    public float minUnitSpacing = 2.0f; // 유닛 간 최소 거리
    public int maxCost = 10;            // 유닛 최대 배치 가능 코스트
    private int currentCost = 0;        // 현재 배치된 유닛의 코스트

    private float dragLiftHeight = 0.2f; // 드래그 중 높이 고정 변수

    public GameObject unitPreviewPrefab;    // 유닛 미리보기 프리팹
    private GameObject currentPreview;      // 현재 미리보기 오브젝트
    private List<GameObject> spawnedUnits = new List<GameObject>();   //배치된 유닛 추적용 리스트
    private GameObject selectedUnit;    // 현재 선택된 유닛
    private Color originalColor;        // 강조 전 색상 저장
    
    private Vector3 dragOffset = Vector3.zero; //유닛과 클릭 지점 사이 거리
    private Vector3 dragStartPos;
    private float dragBaseY = 0f;       //드래그 시작 시 기준 높이
    private bool isDragging = false;    // 드래그 중인지 여부
    private bool isSpawnMode = false;   //유닛 소환 모드 여부

    void Start()
    {
        //UpdateCostUI(); // 초기 UI 업데이트
        
        if(unitPreviewPrefab != null)
        {
            currentPreview = Instantiate(unitPreviewPrefab); // 미리보기 오브젝트 생성);
            currentPreview.SetActive(false); // 초기에는 비활성화
        }
    }

    
    void Update()
    {
        // 마우스 클릭 위치에서 Ray 생성
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit; 
        
        if(Input.GetMouseButtonDown(0))
        {
            if(Physics.Raycast(ray, out hit, 100f, unitLayer))
            {
                GameObject clicked = hit.collider.gameObject;

                if(clicked != null)
                {
                    if(clicked != selectedUnit)
                    {
                        SelectUnit(clicked);
                    }

                    if(selectedUnit != null && clicked == selectedUnit)
                    {
                        isDragging = true;
                        
                        Vector3 hitPointXZ = new Vector3(hit.point.x, 0, hit.point.z);
                        Vector3 unitPosXZ = new Vector3(selectedUnit.transform.position.x, 0, selectedUnit.transform.position.z);
                        dragOffset = unitPosXZ - hitPointXZ;

                        dragBaseY = selectedUnit.transform.position.y;
                        dragStartPos = selectedUnit.transform.position;

                        Rigidbody rb = selectedUnit.GetComponent<Rigidbody>();
                        if(rb != null) rb.isKinematic = true;

                        Collider col = selectedUnit.GetComponent<Collider>();
                        if(col != null) col.isTrigger = true; // 충돌 무시
                    }
                }
            }
        }

        // 드래그 중 : 마우스를 움직이며 따라감
        if(Input.GetMouseButton(0) && isDragging)
        {
            if(Physics.Raycast(ray, out hit, 100f ,canSpawnLayer)) // 바닥 기준 이동
            {
                Vector3 newPos = new Vector3(hit.point.x + dragOffset.x, dragBaseY + dragLiftHeight, hit.point.z + dragOffset.z);
                selectedUnit.transform.position = newPos;
            } 
        }

        // 드래그 종료: 마우스 버튼 떼면 이동 완료
        if(Input.GetMouseButtonUp(0) && isDragging && selectedUnit != null)
        {
            isDragging = false;
            //중복 트윈 방지(선택)
            if(DOTween.IsTweening(selectedUnit.transform)) return;

            Vector3 dropPos = selectedUnit.transform.position;

            Rigidbody rb = selectedUnit.GetComponent<Rigidbody>();
            Collider col = selectedUnit.GetComponent<Collider>();
            
            bool needsCorrection = !CanSpawnHere(dropPos);
            Vector3? validPos = needsCorrection ? FindNearestValidPosition(dropPos, 3f) : null;

            //이동 후 겹칠 시 자동 위치 보정
            Vector3 target;
            if(needsCorrection && validPos.HasValue)
            {
                target = new Vector3(validPos.Value.x, dragBaseY, validPos.Value.z);    
            }
            else if(needsCorrection)
            {
                target = dragStartPos;
            }
            else
            {
                target = dropPos;
            }

            if(col != null) col.isTrigger = true;
            if(rb != null) rb.isKinematic = true;  

            selectedUnit.transform.DOMove(target, 0.2f).SetEase(Ease.OutCubic).OnComplete(() => {
            if(col != null) col.isTrigger = false;
            if(rb != null) rb.isKinematic = false;          
            });
            
        }

        // 유닛 선택
        if(Input.GetMouseButtonDown(0) && !isDragging && !isSpawnMode)
        {
            if(Physics.Raycast(ray, out hit, 100f, unitLayer))
            {
                GameObject clicked = hit.collider.gameObject;

                if(clicked == selectedUnit) 
                {
                    UnselectUnit(); // 같은 유닛 클릭 → 선택 해제   
                }
                else
                {
                    SelectUnit(clicked); // 다른 유닛 선택
                }
                return;
            }
        }

        // 프리뷰
        if(isSpawnMode && Physics.Raycast(ray, out hit, 100f, canSpawnLayer))
        {
           Vector3 preivewPos = hit.point;
             preivewPos.y += 0.5f; // 미리보기 높이 조정

            if(currentPreview != null)
            {
                currentPreview.SetActive(true); // 미리보기 활성화
                currentPreview.transform.position = preivewPos; // 미리보기 위치 설정

                //위치 가능 여부 체크 → 색상 변경
                bool canSpawn = CanSpawnHere(hit.point);  
                SetPreviewColor(canSpawn ? Color.green : Color.red);
            }

            // 실제 유닛 소환
            if (Input.GetMouseButtonDown(0))
            {
                if(CanSpawnHere(hit.point))
                {
                    TrySpawnUnit(hit.point);     // 유닛 소환
                }
                else
                {
                    Debug.Log("소환할 수 없는 위치입니다.");
                }
            }
        }
        else
        {
            if(currentPreview != null)
            {
                currentPreview.SetActive(false);    // 레이캐스트 안 닿으면 숨김김
            }
        }
        

        //선택된 유닛 삭제
        if (Input.GetMouseButtonDown(1) && selectedUnit != null)
        {
           DeleteUnit(selectedUnit);     // 유닛 삭제   
        }
        //유닛 소환 모드
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            isSpawnMode = true;
            if(currentPreview != null)
            {
                currentPreview.SetActive(true);
            }
        }

        //유닛 소환 모드 종료
        if(isSpawnMode && Input.anyKeyDown)
        {
            if(!Input.GetKeyDown(KeyCode.Alpha1))
            {
                isSpawnMode = false;
                if(currentPreview != null)
                {
                    currentPreview.SetActive(false);
                }
            }
        }

    }

    //유닛 해당 지점 소환 가능 여부 체크
    bool CanSpawnHere(Vector3 pos)
    {
        foreach(GameObject unit in spawnedUnits)
        {
            if(unit == null || unit == selectedUnit)
            {
                continue; // null 체크
            } 
            
            float dist = Vector2.Distance(new Vector2(unit.transform.position.x, unit.transform.position.z), new Vector2(pos.x, pos.z));
            if(dist < minUnitSpacing)
            {
                return false; // 너무 가까운 유닛이 존재
            }   
        }
        return true; // 소환 가능   
    }

    //유닛 코스트 체크 후 소환
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

        position.y += 1f; // 소환 높이 조정 

        GameObject unit = Instantiate(unitPrefab, position, Quaternion.identity);
        spawnedUnits.Add(unit); // 소환된 유닛 리스트에 추가      

        currentCost += unitCost; // 현재 코스트 업데이트
        //UpdateCostUI(); // UI 업데이트}
    }

    void SetPreviewColor(Color color)
    {
        Renderer[] renderers = currentPreview.GetComponentsInChildren<Renderer>();
        foreach(Renderer rend in renderers)
        {
            foreach(var mat in rend.materials)
            {
                mat.color = color; // 미리보기 색상 변경
            }
        }    
    }

    void DeleteUnit(GameObject unit)
    {
        UnitCost costCompenet = unit.GetComponent<UnitCost>();
        if(costCompenet != null)
        {
            currentCost -= costCompenet.cost;
            currentCost = Mathf.Max(0, currentCost); // 코스트 0 이하로 안가게
        }

        if(spawnedUnits.Contains(unit))
        {
            spawnedUnits.Remove(unit); // 리스트에서 삭제
        }

        Destroy(unit); // 유닛 삭제
        Debug.Log("유닛 삭제");
    }


    // 유닛 선택 → 강조(노란색)
    void SelectUnit(GameObject unit)
    {
        if(selectedUnit != null)
        {
            UnselectUnit();
        }

        selectedUnit = unit;

        Renderer rend = selectedUnit.GetComponentInChildren<Renderer>();
        if(rend != null)
        {
            rend.material = new Material(rend.material);
            originalColor = rend.material.color;
            rend.material.color = Color.yellow;
        }
    }

    //유닛 선택 해제 → 원래 색 복원
    void UnselectUnit()
    {
        if(selectedUnit == null)
        {
            return;
        }

        Renderer rend = selectedUnit.GetComponentInChildren<Renderer>();
        if(rend != null)
        {
            rend.material.color = originalColor;
        }


        selectedUnit = null;
    }

    // 선택 이동 후 위치 보정 메서드
    Vector3? FindNearestValidPosition(Vector3 center, float maxRadius, int steps = 16)
    {
        float angleStep = 360f / steps;

        for(float radius = 0.5f; radius <= maxRadius; radius += 0.5f)
        {
            for (int i = 0; i < steps; i++)
            {
                float angle = angleStep * i;
                Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;

                Vector3 checkPos = center + offset;

                if(CanSpawnHere(checkPos))
                {
                    return checkPos;
                }
            }
        }

        return null;
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