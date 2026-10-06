using System;
using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.EffectSystem
{
    //[CreateAssetMenu(fileName = "Effect SO", menuName = "Effect/Effect SO", order = 0)]
    public abstract class AbstractEffectSO : ScriptableObject
    {
        [field:SerializeField] public string Name { get; private set; }
        [field:SerializeField] public string Description { get; private set; }
        [field:SerializeField] public Sprite Icon { get; private set; }
        [field:SerializeField] public float EffectDuration { get; private set; }
        [field:SerializeField] public EffectType Type { get; private set; }
        
        protected ModuleOwner Owner { get; private set; }
        protected float EffectStartTime { get; private set; }
        public virtual void EffectStart(ModuleOwner owner)
        {
            Owner = owner;
            EffectStartTime = Time.time;
        }

        public virtual void Update()
        {
            
        }

        public virtual void EffectEnd()
        {
            
        }
        
        public abstract float GetRemainingDuration();

        public static T CloneEffect<T>(T target) where T : AbstractEffectSO
            => Instantiate(target);

    }
}