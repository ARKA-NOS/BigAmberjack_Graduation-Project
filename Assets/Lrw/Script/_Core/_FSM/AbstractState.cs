namespace Lrw.Script._Core._FSM
{
    public abstract class AbstractState : IState
    {
        public abstract void Enter();
        public abstract void StateUpdate();
        public abstract void StateFixedUpdate();
        public abstract void Exit();
    }
}