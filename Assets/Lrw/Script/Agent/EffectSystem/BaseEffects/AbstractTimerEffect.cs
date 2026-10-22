using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.EffectSystem.BaseEffects
{
    public class AbstractTimerEffect : AbstractEffect, ITimedEffect
    {
        private float _effectStartTime;
        public AbstractTimerEffect(ModuleOwner owner,float duration) : base(owner)
        {
            Duration = duration;
        }

        protected override void OnEnter()
        {
            base.OnEnter();
            _effectStartTime = Time.time;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (RemainingTime <= 0f)
            {
                IsEnd = true;
            }
        }

        public float Duration { get; private set; }
        public float RemainingTime => Mathf.Max(0f, _effectStartTime + Duration - Time.time);
        public float NormalizedTime => RemainingTime / Duration;
    }
}