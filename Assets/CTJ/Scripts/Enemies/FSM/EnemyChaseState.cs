namespace CTJ.Enemies.FSM
{
    internal sealed class EnemyChaseState : EnemyState
    {
        public EnemyChaseState(EnemyBase enemy) : base(enemy)
        {
        }

        public override void StateUpdate()
        {
            if (!Enemy.IsTargetInRange(Enemy.DetectionRange))
            {
                Enemy.ChangeState(EnemyStateType.Idle);
                return;
            }

            if (Enemy.IsTargetInRange(Enemy.AttackRange))
                Enemy.ChangeState(EnemyStateType.Attack);
        }

        public override void StateFixedUpdate()
        {
            Enemy.MoveTowardsTarget();
        }

        public override void Exit()
        {
            Enemy.StopHorizontalMovement();
        }
    }
}
