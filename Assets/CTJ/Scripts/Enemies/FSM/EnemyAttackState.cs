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
            // 근접 공격의 준비/타격/후딜 동안은 정지 상태를 유지합니다.
            if (Enemy.IsAttackInProgress)
                return;

            if (!Enemy.IsTargetInRange(Enemy.DetectionRange))
            {
                Enemy.ChangeState(EnemyStateType.Idle);
                return;
            }

            if (!Enemy.IsTargetInRange(Enemy.AttackRange))
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
