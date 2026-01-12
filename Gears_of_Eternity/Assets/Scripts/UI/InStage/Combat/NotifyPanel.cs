using DG.Tweening;
using TMPro;
using UnityEngine;

public class NotifyPanel : MonoBehaviour
{
    public static NotifyPanel Instance { get; private set; }
    
    
    [SerializeField] private TMP_Text notifyMassage;
    [SerializeField] private RectTransform root;

    private Sequence _seq;


    private void Awake()
    {
        Instance = this;
    }
    
    public void ShowNotifyPanel(string massage, float time = 2f)
    {
        Kill();

        notifyMassage.text = massage;
        
        _seq = DOTween.Sequence().SetUpdate(true);
        _seq.Join(GetComponent<CanvasGroup>().DOFade(1f, 0.1f).OnComplete(() => HideNotifyPanel(time)));
    }
    
    private void HideNotifyPanel(float time)
    {
        Kill();

        _seq = DOTween.Sequence().SetUpdate(true);
        _seq.Join(GetComponent<CanvasGroup>().DOFade(0f, 0.2f).SetDelay(time));
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
