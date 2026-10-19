namespace CTJ.Enemies.FSM
{
    internal sealed class EnemyChaseState : EnemyState
    {
        public EnemyChaseState(EnemyBase enemy) : base(enemy)
        {
        }

        public override void StateUpdate()
        {
            if (!Enemy.IsTargetInRange(Enemy.ChaseReleaseRange))
            {
                Enemy.ChangeState(EnemyStateType.Idle);
                return;
            }

            if (Enemy.CanAttackTarget())
                Enemy.ChangeState(EnemyStateType.Attack);
        }

        public override void StateFixedUpdate()
        {
            // 몸통 접촉으로 멈추지 않으므로, 물리 이동 직전에도 공격 조건을 확인합니다.
            if (Enemy.CanAttackTarget())
            {
                Enemy.ChangeState(EnemyStateType.Attack);
                return;
            }
            Enemy.MoveTowardsTarget();
        }

        public override void Exit()
        {
            Enemy.StopHorizontalMovement();
        }
    }
}
