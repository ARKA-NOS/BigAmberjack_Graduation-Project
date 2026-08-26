using System;
using System.Collections.Generic;
using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.EffectSystem
{
    public class EffectModule : Module
    {
        private readonly HashSet<Effect> _effects = new();
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
        }

        private void Update()
        {
            EffectUpdate();
        }

        private void EffectUpdate()
        {
            Queue<Effect> queue = new Queue<Effect>();
            
            foreach (var effect in _effects)
            {
                effect.Update();
                if (effect.RammingEffectTime() >= 0f)
                {
                    queue.Enqueue(effect);
                }
            }

            while (queue.Count > 0)
            {
                var effect = queue.Dequeue();
                RemoveEffect(effect);
            }
            
        }

        public void AddEffect(Effect effect)
        {
            _effects.Add(effect);
            effect.Init(_owner);
        }
        
        public void RemoveEffect(Effect effect)
        {
            _effects.Remove(effect);
            effect.Destroy();
        }
        
        
        
    }
}