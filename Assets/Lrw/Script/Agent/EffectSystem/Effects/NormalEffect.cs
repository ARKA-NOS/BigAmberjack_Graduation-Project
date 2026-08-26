using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.EffectSystem.Effects
{
    public abstract class NormalEffect : Effect
    {
        public NormalEffect(EffectData data,float effectDuration) : base(data, effectDuration)
        {
            
        }

        public override float RammingEffectTime()
        {
            return EffectStartTime + EffectDuration - Time.time;
        }
    }
}