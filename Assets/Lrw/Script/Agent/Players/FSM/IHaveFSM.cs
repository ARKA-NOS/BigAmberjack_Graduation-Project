namespace Lrw.Script.Agent.Players.FSM
{
    public interface IHaveFsm<T>
    {
        void ChangeState(T newState);
    }
}