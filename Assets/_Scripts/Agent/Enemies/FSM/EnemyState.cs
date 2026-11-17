using _Scripts._Core._FSM;

namespace _Scripts.Agent.Enemies.FSM
{
    internal abstract class EnemyState : IState
    {
        protected EnemyBase Enemy { get; }

        protected EnemyState(EnemyBase enemy)
        {
            Enemy = enemy;
        }

        public virtual void Enter()
        {
        }

        public virtual void StateUpdate()
        {
        }

        public virtual void StateFixedUpdate()
        {
        }

        public virtual void Exit()
        {
        }
    }
}
