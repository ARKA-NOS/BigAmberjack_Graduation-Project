namespace _Scripts.Agent.Players.FSM
{
    public interface IHaveFsm<T>
    {
        void ChangeState(T newState);
    }
}