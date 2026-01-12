using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 유닛 머리 위 체력바 UI 뷰.
/// - CombatUIManager가 풀링/위치 갱신을 담당하고
/// - 외부(바인더)가 HP 값 갱신(SetValues)만 호출한다.
/// </summary>
public sealed class HealthPointBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform root;
    [SerializeField] private Image hpFill;
    [SerializeField] private Image barrierFill;

    [Header("Options")]
    [SerializeField] private bool hideWhenOffscreen = true;

    private Transform _target;
    private Vector3 _worldOffset;
    private bool _isBound;

    public bool IsBound => _isBound;

    private void Reset()
    {
        root = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 유닛(월드 트랜스폼)과 바를 연결한다
    /// </summary>
    public void Bind(Transform target, Vector3 worldOffset)
    {
        _target = target;
        _worldOffset = worldOffset;
        _isBound = (target != null);

        if (root != null)
            root.gameObject.SetActive(true);
    }

    public void Unbind()
    {
        _target = null;
        _isBound = false;

        if (root != null)
            root.gameObject.SetActive(false);
    }

    /// <summary>
    /// current/max/barrier를 UI에 반영한다.
    /// barrier는 현재 HP 위로 추가되는 보호막형태로 표시(옵션 이미지 필요)
    /// </summary>
    public void SetValues(float currentHp, float maxHp, float barrier)
    {
        if (maxHp <= 0f)
        {
            if (hpFill != null) hpFill.fillAmount = 0f;
            if (barrierFill != null) barrierFill.fillAmount = 0f;
            return;
        }

        float hp01 = Mathf.Clamp01(currentHp / maxHp);
        float barrierEnd01 = Mathf.Clamp01((currentHp + barrier) / maxHp);

        if (hpFill != null) hpFill.fillAmount = hp01;

        // barrierFill은 "현재HP+보호막"의 끝지점까지 채우는 방식이 일반적이다.
        // 프리팹에서 barrierFill이 hpFill 뒤에 깔리도록 레이어/정렬을 잡으면,
        // 보호막 구간이 HP 위로 자연스럽게 표현된다.
        if (barrierFill != null) barrierFill.fillAmount = barrierEnd01;

        if (barrierFill != null)
            barrierFill.gameObject.SetActive(barrier > 0.0001f);
    }

    /// <summary>
    /// CombatUIManager가 LateUpdate에서 호출
    /// 월드 좌표를 캔버스 로컬 좌표로 변환하여 따라다니게 만든다
    /// </summary>
    public void SyncPosition(
        Camera worldCamera,
        Canvas canvas,
        RectTransform canvasRect,
        Camera canvasEventCamera)
    {
        if (!_isBound || _target == null || root == null) return;
        if (worldCamera == null || canvas == null || canvasRect == null) return;

        Vector3 worldPos = _target.position + _worldOffset;
        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);

        // 카메라 뒤로 가면 숨김 처리
        if (screenPos.z <= 0f)
        {
            if (hideWhenOffscreen) root.gameObject.SetActive(false);
            return;
        }

        bool offscreen = screenPos.x < 0f || screenPos.x > Screen.width || screenPos.y < 0f || screenPos.y > Screen.height;
        if (hideWhenOffscreen)
            root.gameObject.SetActive(!offscreen);

        if (hideWhenOffscreen && offscreen) return;

        // ScreenPoint -> Canvas LocalPoint
        Vector2 localPoint;
        Camera eventCam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvasEventCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, eventCam, out localPoint))
        {
            root.anchoredPosition = localPoint;
        }
    }
}