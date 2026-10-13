namespace Agents.Players.FSM
{
    public interface IHaveFsm<T>
    {
        void ChangeState(T newState);
    }
}