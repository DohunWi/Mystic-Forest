using UnityEngine;

public class EnemyAttackState : MonsterState
{
    private float attackStateDuration = 1.1f; // 공격 모션 시간 (애니메이션 길이와 맞춰야함)

    public EnemyAttackState(EnemyAI enemy, EnemyStateMachine stateMachine, string animBoolName)
        : base(enemy, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        enemy.SetVelocity(0f); // 공격할 땐 멈춤
        enemy.lastAttackTime = Time.time; // 쿨타임 기록
        enemy.PlayAnim("Attack");
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // 공격 시간이 끝나면 다시 Idle이나 Chase로 결정
        if (Time.time >= enemy.startTime + attackStateDuration)
        {
            stateMachine.ChangeState(enemy.ChaseState);
        }
    }
}