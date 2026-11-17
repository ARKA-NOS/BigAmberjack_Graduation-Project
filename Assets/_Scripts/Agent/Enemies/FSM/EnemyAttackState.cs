namespace _Scripts.Agent.Enemies.FSM
{
    internal sealed class EnemyAttackState : EnemyState
    {
        public EnemyAttackState(EnemyBase enemy) : base(enemy)
        {
        }

        public override void Enter()
        {
            Enemy.StopHorizontalMovement();
        }

        public override void StateUpdate()
        {
            // 착지점을 정한 도약은 타깃이 멀어져도 준비부터 대기까지 마칩니다.
            if (Enemy.FinishesAttackOutsideChaseRange) return;
            // 준비 중이어도 추적 해제/타깃 소멸은 먼저 처리합니다.
            if (!Enemy.IsTargetInRange(Enemy.ChaseReleaseRange))
            {
                Enemy.CancelAttack();
                Enemy.ChangeState(EnemyStateType.Idle);
                return;
            }

            if (Enemy.IsAttackInProgress)
                return;

            if (!Enemy.CanAttackTarget())
            {
                Enemy.ChangeState(EnemyStateType.Chase);
                return;
            }

            Enemy.ExecuteAttack();
        }

        public override void StateFixedUpdate()
        {
            Enemy.StopHorizontalMovement();
        }
    }
}

