namespace Agents.Players.FSM
{
    public interface IHaveFsm<T>
    {
        void ChangeState(T newState, float transitionDuration = 0.1f);
    }
}