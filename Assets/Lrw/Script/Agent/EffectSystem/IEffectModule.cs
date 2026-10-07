namespace Lrw.Script.Agent.EffectSystem
{
    public interface IEffectModule
    {
        void Effect(AbstractEffectSO effect);
        void EffectClear(EffectType type);
    }
}