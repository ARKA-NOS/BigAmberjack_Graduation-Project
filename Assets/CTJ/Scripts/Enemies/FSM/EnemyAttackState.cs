using UnityEngine;

namespace CTJ.Enemies.FSM
{
    internal sealed class EnemyAttackState : EnemyState
    {
        private float _attackTimer;

        public EnemyAttackState(EnemyBase enemy) : base(enemy)
        {
        }

        public override void Enter()
        {
            Enemy.StopHorizontalMovement();
            _attackTimer = 0f;
        }

        public override void StateUpdate()
        {
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

            _attackTimer -= Time.deltaTime;
            if (_attackTimer > 0f)
                return;

            Enemy.ExecuteAttack();
            _attackTimer = Enemy.AttackInterval;
        }

        public override void StateFixedUpdate()
        {
            Enemy.StopHorizontalMovement();
        }
    }
}
