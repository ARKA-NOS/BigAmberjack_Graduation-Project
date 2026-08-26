using DevLib.ModuleSystem;
using Lrw.Script._Core;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Agent.EffectSystem.Effects
{
    public class PositionEffect : NormalEffect
    {
        private StatData _statData;
        public PositionEffect(EffectData data,StatData stat, float effectDuration) : base(data, effectDuration)
        {
            _statData = stat;
        }

        public override void Start()
        {
            base.Start();
            Target.GetModule<IStatModule>()
                .GetStat(_statData)
                .AddModify(this,new StatModifyData(0,5f));
        }

        public override void Destroy()
        {
            base.Destroy();
            Target.GetModule<IStatModule>()
                .GetStat(_statData)
                .RemoveModify(this);
        }
    }
}