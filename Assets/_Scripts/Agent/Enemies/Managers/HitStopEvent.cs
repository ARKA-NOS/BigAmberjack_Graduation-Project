using _Scripts._Core._EventSystem;

namespace _Scripts.Agent.Enemies.Managers
{
    public readonly struct HitStopEvent : IEvent
    {
        public float Duration { get; }
        public float StopTimeScale { get; }

        public HitStopEvent(float duration, float stopTimeScale = 0.05f)
        {
            Duration = duration;
            StopTimeScale = stopTimeScale;
        }
    }
}
