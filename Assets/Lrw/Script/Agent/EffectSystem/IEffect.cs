namespace Lrw.Script.Agent.EffectSystem
{
    public interface IEffect
    {
        EffectType Type { get; }
        bool IsEnd { get; }
        void Enter();
        void Update();
        void Exit();
    }
}