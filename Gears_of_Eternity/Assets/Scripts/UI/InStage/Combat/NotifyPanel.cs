using DG.Tweening;
using UnityEngine;

public class NotifyPanel : MonoBehaviour
{
    public static NotifyPanel Instance { get; private set; }
    
    
    [SerializeField] private GameObject notifyPanelPrefab;
    [SerializeField] private RectTransform root;

    private Sequence _seq;


    private void Awake()
    {
        Instance = this;
    }
    
    public void ShowNotifyPanel()
    {
        var o = Instantiate(notifyPanelPrefab, root);
        o.GetComponent<RectTransform>().SetAsLastSibling();
        
        Kill();
        Debug.Log($"[CardUiHandler] NotifyPanel Show");

        //_seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        //_seq.Join(notifyPanelPrefab.GetComponent<CanvasGroup>().DOFade(1f, 0.4f).SetDelay(5f).OnComplete(() => HideNotifyPanel()));
    }
    
    public void HideNotifyPanel()
    {
        Kill();

        _seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        _seq.Join(notifyPanelPrefab.GetComponent<CanvasGroup>().DOFade(0f, 1f));
    }
    
    
    
    private void Kill()
    {
        if (_seq != null)
        {
            _seq.Kill();
            _seq = null;
        }
    }

    private void OnDisable()
    {
        Kill();
    }
}
