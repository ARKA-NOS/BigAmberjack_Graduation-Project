using DevLib.ModuleSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Agent.EffectSystem.BaseEffects
{
    [CreateAssetMenu(fileName = "Stat Effect", menuName = "Effect/Stat Effect", order = 0)]
    public class StatEffect : AbstractEffectSO
    {
        [SerializeField] private StatData targetStat;
        [SerializeField] private int priority;
        [SerializeField] private float value;
        [SerializeField] private ModifyMathType modifyType;
        
        private Stat _stat;
        public override void EffectStart(ModuleOwner owner)
        {
            base.EffectStart(owner);
            IStatModule statModule = owner.GetModule<IStatModule>();
            if(statModule == null) return;
            
            _stat = statModule.GetStat(targetStat);
            
            _stat.SetModify(this,new StatModifyData(priority,value,modifyType));
        }

        public override void EffectEnd()
        {
            base.EffectEnd();
            _stat?.RemoveModify(this);
        }
        
        public override float GetRemainingDuration()
            => EffectDuration - (Time.time - EffectStartTime);
        
    }
}