using Lrw.Script.Agent.EffectSystem.Interface;
using UnityEngine;

namespace Lrw.Script.Agent.EffectSystem.BaseEffects
{
    public abstract class AbstractTemporaryEffect : AbstractEffectSO, ITemporaryEffect
    {
        [field:SerializeField] public float EffectDuration { get; private set; }

        public override bool IsEffectEnd => RemainingEffectDuration() <= 0;

        public abstract float RemainingEffectDuration();
    }
}