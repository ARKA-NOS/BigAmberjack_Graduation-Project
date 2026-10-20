namespace Lrw.Script.Agent.EffectSystem
{
    public interface ITimedEffect
    {
        float Duration { get; }
        float RemainingTime { get; }
        float NormalizedTime { get;}
    }
}