using DevLib.ModuleSystem;
using Lrw.Script.Agent.StatSystem;

namespace Lrw.Script.Agent.EffectSystem.Effects
{
    public class StatEffect : AbstractEffect
    {
        private readonly Stat _stat;
        protected StatModifyData ModifyData;
        
        public StatEffect(EffectType type,ModuleOwner owner, StatData statData ,StatModifyData modifyData) : base(type,owner)
        {
            if(!Owner.TryGetModule(out IStatModule statModule)) return;
            
            _stat = statModule.GetStat(statData);
            
            ModifyData = modifyData;
        }
        
        protected override void OnEnter()
            => UpdateModify();

        protected override void OnUpdate()
            => UpdateModify();

        protected override void OnExit()
        {
            _stat?.RemoveModify(this);
        }

        private void UpdateModify()
            => _stat?.SetModify(this,ModifyData);
        
    }
}