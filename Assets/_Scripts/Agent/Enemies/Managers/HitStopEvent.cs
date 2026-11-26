using _Scripts._Core._EventSystem;

namespace _Scripts.Agent.Enemies.Managers
{
    public readonly struct HitStopEvent : IEvent
    {
        public float Duration { get; }

        public HitStopEvent(float duration) => Duration = duration;
    }
}
