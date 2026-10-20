using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.EffectSystem.BaseEffects
{
    public class AbstractTimerEffect : AbstractEffect,ITimedEffect
    {
        private float _effectStartTime;
        public AbstractTimerEffect(EffectType type, ModuleOwner owner,float duration) : base(type, owner)
        {
            Duration = duration;
        }

        protected override void OnEnter()
        {
            base.OnEnter();
            _effectStartTime = Time.time;
        }

        public float Duration { get; private set; }
        public float RemainingTime => Mathf.Max(0f, _effectStartTime + Duration - Time.time);
        public float NormalizedTime => RemainingTime / Duration;
    }
}