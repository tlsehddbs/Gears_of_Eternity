using UnityEngine;

public class AttackAnimSMB : StateMachineBehaviour
{
    [Range(0f, 1f)]
    public float hitNormalized = 0.35f;

    private bool fired;
    private UnitCombatFSM unit;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        fired = false;
        unit = animator.GetComponentInParent<UnitCombatFSM>();
        if (unit != null)
            unit.SetAttackAnimInProgress(true);
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (unit == null || fired) return;

        float t = stateInfo.normalizedTime % 1f;
        if (t >= hitNormalized)
        {
            fired = true;
            unit.OnAnim_AttackHit();
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (unit != null)
        {
            unit.SetAttackAnimInProgress(false);
            unit.OnAnim_AttackFinished();
        }
    }
}