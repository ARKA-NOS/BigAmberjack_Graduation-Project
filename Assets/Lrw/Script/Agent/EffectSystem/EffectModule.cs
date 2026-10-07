using System;
using System.Collections.Generic;
using System.Linq;
using DevLib.ModuleSystem;
using Lrw.Script._Core;
using Lrw.Script._Core.EnumSystem;
using Lrw.Script.Agent.EffectSystem.BaseEffects;
using Lrw.Script.Agent.EffectSystem.Interface;
using UnityEngine;

namespace Lrw.Script.Agent.EffectSystem
{
    public class EffectModule : Module, IEffectModule
    {
        [SerializeField] private AbstractEffectSO[] startEffects;
        
        private List<AbstractEffectSO> _currentEffects;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _currentEffects = new();
        }

        private void Start()
        {
            foreach (AbstractEffectSO effect in startEffects)
            {
                if(effect == null) continue;
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
            for (int i = _currentEffects.Count - 1; i >= 0; i--)
            {
                AbstractEffectSO effect = _currentEffects[i];
                effect.Update();

                if (effect.IsEffectEnd) _currentEffects.RemoveAt(i);
            } 
            
        }

        public void EffectClear()
        {
            _currentEffects.ForEach(x => x.EffectEnd());
            _currentEffects.Clear();
        }
        
        
        public void EffectClear(EffectType type)
        {
            var targetEffects = _currentEffects.Where(x => x.Type == type);
            targetEffects.Foreach(x => x.EffectEnd());
            _currentEffects.RemoveAll(x => x.Type == type);
        }
        
        
    }
}