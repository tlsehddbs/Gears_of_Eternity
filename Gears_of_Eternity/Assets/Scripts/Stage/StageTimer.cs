using System;
using System.Collections;
using UnityEngine;

public class StageTimer : MonoBehaviour
{
    [SerializeField] private float limitSeconds;

    public float Remaining { get; private set; }
    public bool IsRunning { get; private set; }

    public event Action<float> OnTick;
    public event Action OnExpired;

    private void Awake()
    {
        limitSeconds = GameManager.Instance.combatTime;
    }

    void OnEnable()
    {
        Remaining = limitSeconds;
        IsRunning = true;
        OnTick?.Invoke(Remaining);
    }

    void Update()
    {
        if (!IsRunning)
        {
            return;
        }

        Remaining -= Time.deltaTime;    // 스테이지 플레이 시간이므로 deltaTime 사용

        if (Remaining <= 0f)
        {
            NotifyPanel.Instance.ShowNotifyPanel("시간 종료", 4f);
            
            Remaining = 0f;
            IsRunning = false;

            Invoke(nameof(Wait), 3);
            
            return;
        }

        OnTick?.Invoke(Remaining);
    }

    void Wait()
    {
        OnTick?.Invoke(Remaining);
        OnExpired?.Invoke();
    }
}
