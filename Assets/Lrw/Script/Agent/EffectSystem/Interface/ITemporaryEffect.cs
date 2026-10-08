namespace Lrw.Script.Agent.EffectSystem.Interface
{
    public interface ITemporaryEffect
    {
        float EffectDuration { get; }
        float RemainingEffectDuration();
    }
}