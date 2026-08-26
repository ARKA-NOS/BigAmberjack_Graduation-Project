using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.EffectSystem
{
    public abstract class Effect
    {
        protected ModuleOwner Target;
        protected readonly EffectData Data;
        protected readonly float EffectDuration;
        protected readonly float EffectStartTime;
        
        public Effect(EffectData data,float effectDuration)
        {
            Data = data;
            EffectDuration = effectDuration;
            EffectStartTime = Time.time;
        }

        public abstract float RammingEffectTime();

        public void Init(ModuleOwner target)
        {
            Target = target;
            Start();
        }

        public virtual void Start()
        {
            
        }

        public virtual void Update()
        {
            
        }

        public virtual void Destroy()
        {
            
        }

    }
}