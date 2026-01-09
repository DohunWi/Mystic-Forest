using UnityEngine;

public abstract class MonsterState
{
    protected EnemyAI enemy;
    protected EnemyStateMachine stateMachine;
    protected string animName; 

    public MonsterState(EnemyAI enemy, EnemyStateMachine stateMachine, string animName)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
        this.animName = animName;
    }

    public virtual void Enter()
    {
        enemy.startTime = Time.time;
        
        // SetBool 대신 Play 사용
        // 이 상태에 들어오면 무조건 해당 애니메이션을 처음부터 재생함
        enemy.Anim.Play(animName); 
    }

    public virtual void LogicUpdate() { }
    public virtual void PhysicsUpdate() { }
}