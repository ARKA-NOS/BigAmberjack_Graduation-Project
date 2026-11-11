using System.Collections.Generic;
using _Scripts.DevLib.ModuleSystem;

namespace _Scripts.Agent.EffectSystem
{
    public class EffectModule : Module, IEffectModule
    {
        private Dictionary<AbstractEffectDataSo,List<IEffect>> _effects;

        public IReadOnlyDictionary<AbstractEffectDataSo, List<IEffect>> Effects => _effects;
        
        private Queue<(AbstractEffectDataSo,IEffect)> queue;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            
            _effects = new();
            queue = new();
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

    }
}