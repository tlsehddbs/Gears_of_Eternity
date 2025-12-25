[System.Serializable]
public class RuntimeUnitStats
{
    public float health;
    public float moveSpeed;
    public float attack;
    public float defense;
    public float attackSpeed;
    public float attackDistance;
    public float damageReduction;
    public float barrier;
    public int guardCount = 0; // 남은 방어(가드) 횟수
    public float criticalChance;
}