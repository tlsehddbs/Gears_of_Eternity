using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public class UIPopupAnimator : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private RectTransform panel;       // 실제 팝업 패널
    [SerializeField] private CanvasGroup group;         // 패널 페이드용 

    [Header("Show")] 
    [SerializeField] private float showDuration = 0.14f;
    [SerializeField] private float showFormYOffset = -120f;
    [SerializeField] private Ease showEase = Ease.OutCubic;
    
    [Header("Hide")]
    [SerializeField] private float hideDuration = 0.10f;
    [SerializeField] private float hideToYOffset = -120f;
    [SerializeField] private Ease hideEase = Ease.InCubic;

    [Header("Options")] 
    [SerializeField] private bool useUnscaledTime = true;

    private Sequence _seq;
    private Vector2 _originPos;

    private void Awake()
    {
        if (panel == null)
        {
            panel = transform as RectTransform;
        }

        if (group == null && panel != null)
        {
            if (!panel.TryGetComponent(out group))
            {
                group = panel.gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        if (panel != null)
        {
            _originPos = panel.anchoredPosition;
        }
        
        // 초기 값은 Hide 상태로 (인스턴스 직후 깜빡임 방지)
        SetInstantHidden();
    }

    public void Show()
    {
        Kill();

        if (panel != null)
        {
            panel.anchoredPosition = _originPos + new Vector2(0f, showFormYOffset);
            panel.SetAsLastSibling();
        }
        
        if (group != null)
        {
            group.alpha = 0f;
            group.blocksRaycasts = true;
            group.interactable = true;
        }
        
        _seq = DOTween.Sequence().SetUpdate(useUnscaledTime).SetLink(gameObject, LinkBehaviour.KillOnDisable);

        if (group != null)
        {
            _seq.Join(group.DOFade(1f, showDuration));
        }

        if (panel != null)
        {
            _seq.Join(panel.DOAnchorPos(_originPos, showDuration).SetEase(showEase));
        }
    }

    public void Hide(Action onComplete = null)
    {
        Kill();

        _seq = DOTween.Sequence().SetUpdate(useUnscaledTime).SetLink(gameObject, LinkBehaviour.KillOnDisable);

        if (panel != null)
        {
            var target = _originPos + new Vector2(0f, hideToYOffset);
            _seq.Join(panel.DOAnchorPos(target, hideDuration).SetEase(hideEase));
        }

        if (group != null)
        {
            _seq.Join(group.DOFade(0f, hideDuration));
        }

        _seq.OnComplete(() => onComplete?.Invoke());
    }

    private void SetInstantHidden()
    {
        if (panel != null)
        {
            panel.anchoredPosition = _originPos;
        }
        
        if (group != null)
        {
            group.alpha = 0f;
            group.blocksRaycasts = true;
            group.interactable = true;
        }
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
