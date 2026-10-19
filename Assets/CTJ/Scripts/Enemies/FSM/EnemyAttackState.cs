namespace CTJ.Enemies.FSM
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
