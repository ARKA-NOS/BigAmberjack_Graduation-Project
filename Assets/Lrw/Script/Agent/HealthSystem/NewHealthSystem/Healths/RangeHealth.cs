using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.StatSystem;
using Lrw.Script.DataStructure;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths
{
    public class RangeHealth : AbstractHealth
    {
        private readonly Stat _maxHealthStat;
        
        protected float MaxHealthValue => _maxHealthStat.Value;
        
        public override Gauge HpGauge => new Gauge(HP, MaxHealthValue);
        protected Color CurrentColor;
        protected HealthType HealthType;
        public RangeHealth(int priority, ModuleOwner owner, HealthType type, Color color, StatData maxStatData) : base(priority,owner)
        {
            FDebug.Assert(owner.TryGetModule(out IStatModule statModule),
                "[RangeHealth] owner is not found IStatModule");

            _maxHealthStat = statModule.GetStat(maxStatData);

            CurrentColor = color;
            
            HealthType = type;
        }

        public override Color GetHealthColor() => CurrentColor;
        public override HealthType GetHealthType() => HealthType;

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