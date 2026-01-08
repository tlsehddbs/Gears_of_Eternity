using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class OpenUnitUpgradePopupOnDoubleClick : MonoBehaviour, IPointerClickHandler
{
    [Header("Popup")] 
    [SerializeField] private UnitUpgradePopupSpawner spawner;

    [Header("DoubleClick")]
    [SerializeField] private float doubleCLickTime = 0.28f;

    private float _lastClickTime = -999f;

    public RuntimeUnitCardRef cardRef;

    public void SetSpawner(UnitUpgradePopupSpawner target)
    {
        spawner = target;
    }

    private void Awake()
    {
        if (spawner == null)
        {
            spawner = FindAnyObjectByType<UnitUpgradePopupSpawner>();
        }
        
        cardRef = GetComponent<RuntimeUnitCardRef>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (spawner == null)
        {
            return;
        }

        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        
        float now = Time.unscaledTime;
        bool isDoubleCLick = (now - _lastClickTime <= doubleCLickTime);
        _lastClickTime = now;

        if (!isDoubleCLick)
        {
            return;
        }
        
        Debug.Log($"CardRef : {cardRef.Card.unitName}");

        spawner.SpawnForTarget(cardRef.Card);
    }
}
