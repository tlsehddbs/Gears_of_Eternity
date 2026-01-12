using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 UI 총괄 매니저.
/// - Canvas 1개(권장)에 붙여서 체력바/데미지팝업을 풀링으로 관리.
/// - 유닛 수가 늘어도 Instantiate/Destroy를 최소화한다.
/// </summary>
public sealed class CombatUIManager : MonoBehaviour
{
    public static CombatUIManager Instance { get; private set; }

    [Header("Canvas")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform canvasRect;

    [Header("Cameras")]
    [SerializeField] private Camera worldCamera;       // 월드 -> 스크린 변환용 (보통 Main Camera)
    [SerializeField] private Camera canvasEventCamera; // ScreenSpaceCamera면 canvas.worldCamera 또는 동일 카메라

    [Header("Prefabs")]
    [SerializeField] private HealthPointBar healthBarPrefab;
    [SerializeField] private DamagePopupView damagePopupPrefab;

    [Header("Pooling")]
    [SerializeField] private int prewarmHealthBars = 32;
    [SerializeField] private int prewarmPopups = 64;

    [Header("Default Offsets")]
    [SerializeField] private Vector3 defaultHealthBarWorldOffset = new Vector3(0f, 2.0f, 0f);
    [SerializeField] private Vector3 defaultPopupWorldOffset = new Vector3(0f, 2.1f, 0f);

    [System.Serializable]
    public struct PopupStyleEntry
    {
        public DamageKind kind;
        public DamagePopupView.Style style;
    }

    [Header("Popup Styles")]
    [SerializeField] private DamagePopupView.Style normalStyle;
    [SerializeField] private DamagePopupView.Style criticalStyle;
    [SerializeField] private DamagePopupView.Style dotStyle;
    [SerializeField] private DamagePopupView.Style healStyle;
    [SerializeField] private List<PopupStyleEntry> overrideStyles = new List<PopupStyleEntry>();

    private readonly Queue<HealthPointBar> _healthBarPool = new Queue<HealthPointBar>();
    private readonly List<HealthPointBar> _activeHealthBars = new List<HealthPointBar>();

    private readonly Queue<DamagePopupView> _popupPool = new Queue<DamagePopupView>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvasRect == null && canvas != null) canvasRect = canvas.GetComponent<RectTransform>();

        // 카메라 자동 보정(미지정시)
        if (worldCamera == null) worldCamera = Camera.main;

        if (canvasEventCamera == null && canvas != null)
        {
            // ScreenSpaceOverlay는 null을 사용하고,
            // ScreenSpaceCamera/WorldSpace는 Canvas에 지정된 worldCamera를 쓰는 편이 안전하다.
            canvasEventCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        // 중요: style.format이 비어 있으면 prefix/suffix 기반으로 1회 생성해서
        // DamagePopupView가 SetText(format, 숫자)만 쓰도록 만든다.
        NormalizePopupStyles();

        Prewarm();
    }

    private void Prewarm()
    {
        if (healthBarPrefab != null)
        {
            for (int i = 0; i < prewarmHealthBars; i++)
            {
                var v = Instantiate(healthBarPrefab, canvasRect);
                v.Unbind();
                _healthBarPool.Enqueue(v);
            }
        }

        if (damagePopupPrefab != null)
        {
            for (int i = 0; i < prewarmPopups; i++)
            {
                var v = Instantiate(damagePopupPrefab, canvasRect);
                v.StopAndHide();
                _popupPool.Enqueue(v);
            }
        }
    }

    private void LateUpdate()
    {
        // 체력바는 항상 유닛을 따라다녀야 하므로 매 프레임 갱신(LateUpdate 권장)
        for (int i = _activeHealthBars.Count - 1; i >= 0; i--)
        {
            var bar = _activeHealthBars[i];
            if (bar == null || !bar.IsBound)
            {
                _activeHealthBars.RemoveAt(i);
                continue;
            }

            bar.SyncPosition(worldCamera, canvas, canvasRect, canvasEventCamera);
        }
    }

    // ------------------------
    // HealthBar
    // ------------------------

    public HealthPointBar SpawnHealthBar(Transform target, Vector3? worldOffset = null)
    {
        if (healthBarPrefab == null || canvasRect == null) return null;

        HealthPointBar v = (_healthBarPool.Count > 0) ? _healthBarPool.Dequeue() : Instantiate(healthBarPrefab, canvasRect);
        v.Bind(target, worldOffset ?? defaultHealthBarWorldOffset);

        if (!_activeHealthBars.Contains(v))
            _activeHealthBars.Add(v);

        // 스폰 직후 한번 위치를 맞춰준다(첫 프레임 튐 방지)
        v.SyncPosition(worldCamera, canvas, canvasRect, canvasEventCamera);

        return v;
    }

    public void DespawnHealthBar(HealthPointBar view)
    {
        if (view == null) return;

        _activeHealthBars.Remove(view);
        view.Unbind();
        view.transform.SetParent(canvasRect, false);
        _healthBarPool.Enqueue(view);
    }

    // ------------------------
    // Damage Popup
    // ------------------------

    public void SpawnDamagePopup(Vector3 worldPosition, float amount, DamageKind kind)
    {
        if (amount <= 0f) return;
        SpawnPopupInternal(worldPosition + defaultPopupWorldOffset, amount, isHeal: false, kind: kind);
    }

    public void SpawnHealPopup(Vector3 worldPosition, float amount)
    {
        if (amount <= 0f) return;
        SpawnPopupInternal(worldPosition + defaultPopupWorldOffset, amount, isHeal: true, kind: DamageKind.Unknown);
    }

    private void SpawnPopupInternal(Vector3 worldPosition, float amount, bool isHeal, DamageKind kind)
    {
        if (damagePopupPrefab == null || canvasRect == null || worldCamera == null || canvas == null) return;

        // 월드 -> 스크린
        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPosition);
        if (screenPos.z <= 0f) return; // 카메라 뒤면 표시 안 함

        // 스크린 -> 캔버스 로컬
        Vector2 localPoint;
        Camera eventCam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvasEventCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, eventCam, out localPoint))
            return;

        DamagePopupView v = (_popupPool.Count > 0) ? _popupPool.Dequeue() : Instantiate(damagePopupPrefab, canvasRect);

        DamagePopupView.Style style = ResolveStyle(isHeal, kind);
        v.Play(localPoint, amount, isHeal, style, OnPopupFinished);
    }

    private void OnPopupFinished(DamagePopupView view)
    {
        if (view == null) return;

        view.StopAndHide();
        view.transform.SetParent(canvasRect, false);
        _popupPool.Enqueue(view);
    }

    private DamagePopupView.Style ResolveStyle(bool isHeal, DamageKind kind)
    {
        if (isHeal) return healStyle;

        // override 우선
        for (int i = 0; i < overrideStyles.Count; i++)
        {
            if (overrideStyles[i].kind == kind)
                return overrideStyles[i].style;
        }

        // 기본 매핑
        switch (kind)
        {
            case DamageKind.Critical:
                return criticalStyle;

            case DamageKind.Dot_Poison:
            case DamageKind.Dot_Bleed:
                return dotStyle;

            case DamageKind.Normal:
            default:
                return normalStyle;
        }
    }

    // ------------------------
    // Style Normalize (중요)
    // ------------------------

    private void NormalizePopupStyles()
    {
        normalStyle = NormalizeStyle(normalStyle, isHeal: false);
        criticalStyle = NormalizeStyle(criticalStyle, isHeal: false);
        dotStyle = NormalizeStyle(dotStyle, isHeal: false);
        healStyle = NormalizeStyle(healStyle, isHeal: true);

        for (int i = 0; i < overrideStyles.Count; i++)
        {
            PopupStyleEntry entry = overrideStyles[i];
            entry.style = NormalizeStyle(entry.style, isHeal: false);
            overrideStyles[i] = entry;
        }
    }

    private DamagePopupView.Style NormalizeStyle(DamagePopupView.Style style, bool isHeal)
    {
        // format이 이미 입력되어 있으면 그대로 사용
        if (!string.IsNullOrEmpty(style.format))
            return style;

        // format이 비어 있으면 prefix/suffix로 1회 생성
        string prefix = style.prefix ?? string.Empty;
        string suffix = style.suffix ?? string.Empty;

        // 힐은 +를 기본 포함
        style.format = isHeal ? $"{prefix}+{{0}}{suffix}" : $"{prefix}{{0}}{suffix}";
        return style;
    }
}