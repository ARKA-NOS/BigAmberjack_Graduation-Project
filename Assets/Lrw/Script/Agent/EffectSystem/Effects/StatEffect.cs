using DevLib.ModuleSystem;
using Lrw.Script.Agent.EffectSystem.BaseEffects;
using Lrw.Script.Agent.StatSystem;

namespace Lrw.Script.Agent.EffectSystem.Effects
{
    public class StatEffect : AbstractTimerEffect
    {
        private readonly Stat _stat;
        protected StatModifyData ModifyData;
        
        public StatEffect(ModuleOwner owner, float duration, StatData statData, StatModifyData modifyData) : base(owner, duration)
        {
            if(!Owner.TryGetModule(out IStatModule statModule)) return;
            
            _stat = statModule.GetStat(statData);
            
            ModifyData = modifyData;
        }
          
        protected override void OnEnter()
        {
            base.OnEnter();
            UpdateModify();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            UpdateModify();
        }

        protected override void OnExit()
        {
            base.OnExit();
            _stat?.RemoveModify(this);
        }

        private void UpdateModify()
            => _stat?.SetModify(this,ModifyData);
        
    }
}