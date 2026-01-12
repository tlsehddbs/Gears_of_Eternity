using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 떠오르는 데미지/회복 숫자 UI.
/// - CombatUIManager에서 Spawn 후 Play를 호출하면,
///   내부 코루틴으로 이동/페이드 애니메이션 실행 후 풀로 반환된다.
/// </summary>
public sealed class DamagePopupView : MonoBehaviour
{
    [Serializable]
    public struct Style
    {
        public Color color;
        public float fontSizeMultiplier;

        // 기존 인스펙터 값 유지용(씬/프리팹 깨짐 방지)
        public string prefix;
        public string suffix;

        // TMP SetText 최적화 경로를 타기 위한 포맷 문자열(권장: {0} 한 개만 사용)
        // 예: "CRIT {0}", "{0}", "+{0}", "POISON {0}"
        public string format;
    }

    [Header("UI References")]
    [SerializeField] private RectTransform root;
    [SerializeField] private TMP_Text text;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation")]
    [SerializeField] private float lifeTime = 0.8f;
    [SerializeField] private float floatUpPixels = 50f;
    [SerializeField] private float fadeOutStart01 = 0.55f;

    private float _baseFontSize;
    private Coroutine _routine;
    private Action<DamagePopupView> _onFinished;

    private void Reset()
    {
        root = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        text = GetComponentInChildren<TMP_Text>();
    }

    private void Awake()
    {
        if (text != null) _baseFontSize = text.fontSize;
    }

    public void StopAndHide()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 1f;
        if (root != null) root.gameObject.SetActive(false);

        _onFinished = null;
    }

    /// <summary>
    /// CombatUIManager에서 세팅 후 실행.
    /// startPos는 Canvas 로컬 좌표(anchoredPosition) 기준.
    /// </summary>
    public void Play(Vector2 startPos, float amount, bool isHeal, Style style, Action<DamagePopupView> onFinished)
    {
        _onFinished = onFinished;

        if (root != null)
        {
            root.anchoredPosition = startPos;
            root.gameObject.SetActive(true);
        }

        // 수치 반올림(원하면 소수점 표기로 교체 가능)
        int v = Mathf.RoundToInt(amount);
        if (v < 0) v = 0;

        if (text != null)
        {
            text.color = style.color;

            float mult = (style.fontSizeMultiplier <= 0f) ? 1f : style.fontSizeMultiplier;
            text.fontSize = _baseFontSize * mult;

            // TMP SetText에 string(prefix/suffix)을 인자로 넘기지 않는다.
            // format 문자열 안에 prefix/suffix를 포함시키고, 숫자만 인자로 넘긴다.
            string fmt = style.format;

            // 안전장치: format이 비어 있으면 prefix/suffix 기반으로 즉석 생성
            if (string.IsNullOrEmpty(fmt))
            {
                string prefix = style.prefix ?? string.Empty;
                string suffix = style.suffix ?? string.Empty;

                // isHeal은 외부에서 스타일 선택에 의해 결정되지만, 포맷이 비어있을 때만 보정해준다.
                fmt = isHeal ? $"{prefix}+{{0}}{suffix}" : $"{prefix}{{0}}{suffix}";
            }

            text.SetText(fmt, v);
        }

        if (canvasGroup != null) canvasGroup.alpha = 1f;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(AnimRoutine(startPos));
    }

    private System.Collections.IEnumerator AnimRoutine(Vector2 startPos)
    {
        float t = 0f;

        while (t < lifeTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / lifeTime);

            // 위로 떠오르기
            float y = floatUpPixels * p;
            if (root != null)
                root.anchoredPosition = startPos + new Vector2(0f, y);

            // 페이드 아웃
            if (canvasGroup != null)
            {
                if (p >= fadeOutStart01)
                {
                    float fp = Mathf.InverseLerp(fadeOutStart01, 1f, p);
                    canvasGroup.alpha = 1f - fp;
                }
            }

            yield return null;
        }

        _routine = null;
        _onFinished?.Invoke(this);
    }
}