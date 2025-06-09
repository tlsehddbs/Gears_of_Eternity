using UnityEngine;
using UnitSkillTypes.Enums;


public abstract class UnitState
{
    protected UnitCombatFSM unit;
    
    
    public UnitState(UnitCombatFSM unit) // 부모 생성자 
    {
        this.unit = unit;
    }

    public virtual void Enter(){}
    public virtual void Update(){}
    public virtual void Exit(){}

}
