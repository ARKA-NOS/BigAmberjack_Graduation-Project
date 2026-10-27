using System;
using System.Collections.Generic;
using DevLib.ModuleSystem;
using Lrw.Script.UI.PlayerUI.HUD_UI.Effect_UI_System;
using UnityEngine;

namespace Lrw.Script.Agent.EffectSystem
{
    public class EffectModule : Module
    {
        [SerializeField] private EffectViewModel effectViewModel;

        private EffectList _effectList;
        
        private Dictionary<AbstractEffectDataSo,List<IEffect>> _effects;
        
        private Queue<(AbstractEffectDataSo,IEffect)> queue;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            
            _effects = new();
            queue = new();
        }

        private void Start()
        {
            _effectList = new EffectList(_effects);

            if (effectViewModel != null)
            {
                effectViewModel.Bind(_effectList);
                ViewModelUpdate();
            }
        }

        public void TakeEffect(AbstractEffectDataSo effectDataSo)
        {
            if(effectDataSo == null) return;
            
            IEffect effect = effectDataSo.CreateEffect(_owner);
            
            effect.Enter();
            
            queue.Enqueue((effectDataSo, effect));
        }

        private void Update()
        {
            QueueUpdate();
            EffectUpdate();
            ViewModelUpdate();
        }
        
        private void QueueUpdate()
        {
            while (queue.Count > 0)
            {
                (AbstractEffectDataSo effectDataSo,IEffect effect) 
                    data = queue.Dequeue();
                
                if (!_effects.TryGetValue(data.effectDataSo, out List<IEffect> effects))
                {
                    effects = new List<IEffect>();
                    _effects.Add(data.effectDataSo, effects);
                }
                
                effects.Add(data.effect);
            }
        }
        
        private void EffectUpdate()
        {
            foreach (List<IEffect> effectList in _effects.Values)
            {
                int count = effectList.Count;
                for (int i = count - 1; i >= 0; i--)
                {
                    IEffect currentEffect = effectList[i];
                    currentEffect.Update();

                    if (!currentEffect.IsEnd) continue;
                    
                    currentEffect.Exit();
                    effectList.RemoveAt(i);
                }
            }
        }
        
        private void ViewModelUpdate()
        {
            if(effectViewModel == null || _effectList == null) return;
            
            _effectList.Update();
        }
        
    }
}