using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.StatSystem;
using Lrw.Script.DataStructure;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths
{
    public class RangeHealth : AbstractHealth, IRangeHealth
    {
        private readonly Stat _maxHealthStat;
        
        private float MaxHealthValue => _maxHealthStat.Value;
        
        public Gauge Health => new(HP, MaxHealthValue);
        
        public RangeHealth(ModuleOwner owner,StatData maxStatData) : base(owner)
        {
            FDebug.Assert(owner.TryGetModule(out IStatModule statModule),
                "[RangeHealth] owner is not found IStatModule");

            _maxHealthStat = statModule.GetStat(maxStatData);
        }


        public override float ApplyHeal(float heal)
        {
            float newHealth = Mathf.Clamp(HP + heal, 0f, MaxHealthValue);
            float reaming = heal - (newHealth - HP);
            HP = newHealth;
            return reaming;
        }

        public override void SetHp(float hp)
        {
            base.SetHp(Mathf.Clamp(hp, 0f, MaxHealthValue));
        }

        
    }
}