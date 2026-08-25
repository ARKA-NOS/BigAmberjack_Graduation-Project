namespace Script._Core._FSM
{
    public interface IState
    {
        void Enter();
        void StateUpdate();
        void StateFixedUpdate();
        void Exit() ;
    }
}