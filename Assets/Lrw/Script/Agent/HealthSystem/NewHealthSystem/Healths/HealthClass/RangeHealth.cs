using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths.HealthSO;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths.HealthClass
{
    public class RangeHealth : AbstractHealth
    {
        private Stat _maxHealth;
        private readonly StatData _maxStatData;

        public RangeHealth(RangeHealthSo healthSo, ModuleOwner owner, float baseHp = 0) : base(healthSo, owner, baseHp)
        {
            _maxStatData = healthSo.MaxHealthStatData;
        }

        public float MaxHealth => _maxHealth?.Value ?? 0f;
        

        public override void Init()
        {
            base.Init();
            FDebug.Assert(Owner.TryGetModule(out IStatModule statModule),
                "[RangeHealth] owner is not have IStatModule");

            _maxHealth = statModule.GetStat(_maxStatData);
        }

        public override HealthChangeData ApplyHeal(HealthChangeData heal)
        {
            float currentHp = Mathf.Clamp(Hp + heal.Amount, 0, MaxHealth);
            heal.Amount -= currentHp - Hp;
            Hp = currentHp;
            return heal;
        }
        
    }
}