using System.Collections;
using UnityEngine;

/// <summary>
/// 유닛(UnitCombatFSM)과 전투 UI(CombatUIManager)를 연결하는 바인더.
/// - 유닛 프리팹 루트(= UnitCombatFSM가 붙은 오브젝트)에 붙이는 것을 권장.
/// - Update를 쓰지 않고 이벤트로만 갱신한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class UnitUIBinder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UnitCombatFSM unit;

    [Header("Offsets")]
    [SerializeField] private Vector3 healthBarWorldOffset = new Vector3(0f, 7.0f, 0f);

    [Header("Options")]
    [SerializeField] private bool showPopupOnBarrierOnly = true;   // HP가 안 깎이고 방어막만 깎여도 숫자를 띄울지
    [SerializeField] private bool hideHealthBarWhenKilled = true;  // 사망 시 체력바를 즉시 숨길지

    private HealthPointBar boundHealthBar;
    private bool isSubscribed;
    private Coroutine bindRoutine;

    private void Reset()
    {
        unit = GetComponent<UnitCombatFSM>();
    }

    private void Awake()
    {
        if (unit == null)
            unit = GetComponent<UnitCombatFSM>();
    }

    private void OnEnable()
    {
        // 오브젝트 풀링/재활성화 대응
        TryBind();
    }

    private void Start()
    {
        // 씬 로딩 순서(매니저 생성 타이밍) 때문에 OnEnable 시점에 못 붙는 케이스 보완
        TryBind();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void TryBind()
    {
        if (bindRoutine != null) return;
        bindRoutine = StartCoroutine(BindWhenReady());
    }

    private IEnumerator BindWhenReady()
    {
        // 1) 필수 컴포넌트 체크
        if (unit == null)
            unit = GetComponent<UnitCombatFSM>();

        if (unit == null)
        {
            bindRoutine = null;
            yield break;
        }

        // 2) CombatUIManager 준비될 때까지 대기
        while (CombatUIManager.Instance == null)
            yield return null;

        // 3) HealthBar 스폰(한 번만)
        if (boundHealthBar == null)
            boundHealthBar = CombatUIManager.Instance.SpawnHealthBar(unit.transform, healthBarWorldOffset);

        // 4) 이벤트 구독(중복 방지)
        SubscribeIfNeeded();

        // 5) 유닛 스탯/HP 초기화가 끝난 뒤 스냅샷 발행
        //    UnitCombatFSM이 Start에서 CloneStats 후 currentHP 세팅하는 구조라, 한 프레임 정도 기다리는 게 안전하다.
        yield return null;
        unit.PublishHpSnapshot();

        bindRoutine = null;
    }

    private void SubscribeIfNeeded()
    {
        if (isSubscribed) return;

        unit.OnHpChanged += HandleHpChanged;
        unit.OnDamageApplied += HandleDamageApplied;
        unit.OnHealed += HandleHealed;

        isSubscribed = true;
    }

    private void Unbind()
    {
        if (bindRoutine != null)
        {
            StopCoroutine(bindRoutine);
            bindRoutine = null;
        }

        if (unit != null && isSubscribed)
        {
            unit.OnHpChanged -= HandleHpChanged;
            unit.OnDamageApplied -= HandleDamageApplied;
            unit.OnHealed -= HandleHealed;

            isSubscribed = false;
        }

        if (boundHealthBar != null && CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.DespawnHealthBar(boundHealthBar);
            boundHealthBar = null;
        }
    }

    private void HandleHpChanged(HpSnapshot snapshot)
    {
        if (boundHealthBar == null) return;
        boundHealthBar.SetValues(snapshot.CurrentHp, snapshot.MaxHp, snapshot.Barrier);
    }

    private void HandleDamageApplied(DamageResult result)
    {
        if (CombatUIManager.Instance == null) return;

        // 표시 수치 결정:
        // - HP가 깎였으면 HpDamage 우선
        // - HP는 안 깎이고 방어막만 깎였으면 BarrierAbsorbed(옵션)
        // - 그 외에는 MitigatedDamage(안전망)
        float amountToShow = 0f;

        if (result.HpDamage > 0f)
        {
            amountToShow = result.HpDamage;
        }
        else if (showPopupOnBarrierOnly && result.BarrierAbsorbed > 0f)
        {
            amountToShow = result.BarrierAbsorbed;
        }
        else if (result.MitigatedDamage > 0f)
        {
            amountToShow = result.MitigatedDamage;
        }

        if (amountToShow > 0f)
            CombatUIManager.Instance.SpawnDamagePopup(unit.transform.position, amountToShow, result.Kind);

        if (hideHealthBarWhenKilled && result.IsKilled)
        {
            // 사망하면 즉시 바 제거(원하면 꺼도 됨)
            if (boundHealthBar != null)
            {
                CombatUIManager.Instance.DespawnHealthBar(boundHealthBar);
                boundHealthBar = null;
            }

            // 이후 이벤트로 UI가 다시 생성/갱신되는 걸 막고 싶으면 구독도 해제
            Unbind();
        }
    }

    private void HandleHealed(HealResult result)
    {
        if (CombatUIManager.Instance == null) return;
        if (result.AppliedAmount <= 0f) return;

        CombatUIManager.Instance.SpawnHealPopup(unit.transform.position, result.AppliedAmount);
    }
}