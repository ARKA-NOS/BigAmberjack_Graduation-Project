using System;
using System.Collections.Generic;
using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.EffectSystem
{
    public class EffectModule : Module, IEffectModule
    {
        [SerializeField] private AbstractEffectSO[] startEffects;
        
        private List<AbstractEffectSO> _currentEffects;
        
        public override void Initialize(ModuleOwner owner)
        {
            _currentEffects = new();
        }

        private void Start()
        {
            foreach (AbstractEffectSO effect in startEffects)
            {
                Effect(effect);
            }
        }

        public void Effect(AbstractEffectSO effect)
        {
            AbstractEffectSO effectClone = AbstractEffectSO.CloneEffect(effect);
            effectClone.EffectStart(_owner);
            _currentEffects.Add(effectClone);
        }

        private void Update()
        {
            List<AbstractEffectSO> removeList = new();
            
            foreach (AbstractEffectSO effect in _currentEffects)
            {
                effect.Update();
                if (effect.GetRemainingDuration() > 0) continue;
                effect.EffectEnd();
                removeList.Add(effect);
            }
            
            foreach (AbstractEffectSO effect in removeList)
            {
                _currentEffects.Remove(effect);
            }
            
        }
    }
}