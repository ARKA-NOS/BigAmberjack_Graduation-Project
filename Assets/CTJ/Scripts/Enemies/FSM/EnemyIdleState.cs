namespace CTJ.Enemies.FSM
{
    internal sealed class EnemyIdleState : EnemyState
    {
        public EnemyIdleState(EnemyBase enemy) : base(enemy)
        {
        }

        public override void Enter()
        {
            Enemy.StopHorizontalMovement();
        }

        public override void StateUpdate()
        {
            if (Enemy.IsTargetInRange(Enemy.DetectionRange))
                Enemy.ChangeState(EnemyStateType.Chase);
        }

        public override void StateFixedUpdate()
        {
            Enemy.StopHorizontalMovement();
        }
    }
}
