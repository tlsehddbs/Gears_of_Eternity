using UnityEngine;

public static class ShopCardLockUtil
{
    public static void Apply(GameObject cardGO, bool locked, float lockedAlpha = 0.65f)
    {
        var overlay = cardGO.transform.Find("DisableOverlay");
        if (overlay != null)
        {
            overlay.gameObject.SetActive(locked);
            overlay.SetAsLastSibling();
        }
        
        var cg = cardGO.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = cardGO.AddComponent<CanvasGroup>();
        }
        
        cg.interactable = !locked;
        cg.blocksRaycasts = !locked;
        
        cg.alpha = locked ? lockedAlpha : 1f;
    }
}
