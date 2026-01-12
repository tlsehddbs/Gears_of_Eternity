using UnityEngine;
using UnityEngine.UI;

public class TimerUI : MonoBehaviour
{
    [SerializeField] private StageTimer timer;
    [SerializeField] private Image fillImage;

    void OnEnable()
    {
        timer.OnTick += OnTick;
        OnTick(timer.Remaining);
    }

    void OnDisable()
    {
        timer.OnTick -= OnTick;
    }

    void OnTick(float remaining)
    {
        float t = Mathf.Clamp01(remaining / Mathf.Max(0.0001f, timerDuration));
        fillImage.fillAmount = t;
    }

    float timerDuration => GetDuration(timer);

    // StageCountdown에 Duration 프로퍼티가 있으면 그걸 쓰는 게 제일 좋음
    static float GetDuration(StageTimer t)
    {
        return t.Duration; 
    }
}
