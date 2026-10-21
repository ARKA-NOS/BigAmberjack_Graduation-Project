namespace Lrw.Script.Agent.EffectSystem
{
    public interface IEffect
    {
        bool IsEnd { get; }
        void Enter();
        void Update();
        void Exit();
    }
}