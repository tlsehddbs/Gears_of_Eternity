using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CardDragController : MonoBehaviour
{
    public static CardDragController Instance;

    [SerializeField] private LayerMask dropLayerMask;

    void Awake()
    {
        Instance = this;
    }

    public bool TryGetDropPosition(out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, dropLayerMask))
        {
            worldPosition = hit.point;
            return true;
        }

        return false;
    }
}