using System;
using _Scripts.Agent.StatSystem;
using _Scripts.DevLib.ModuleSystem;
using _Scripts.Upgrade.Upgrader;
using UnityEngine;

namespace _Scripts.Upgrade.SO.Upgrades
{
    [CreateAssetMenu(fileName = "Upgrader System Data", menuName = "Upgrade/Upgrade Data/Upgrader System Data", order = 0)]
    public class UpgraderSystemData : AbstractUpgradeData
    {
        [Serializable]
        private struct StatBonus
        {
            [field: SerializeField] public StatData Stat { get; private set; }
            [field: SerializeField] public StatModifyData Modify { get; private set; }
        }

        [SerializeField] private UpgraderSO upgrader;
        [SerializeField] private StatBonus[] statBonuses;

        public override void AgentUpgrade(ModuleOwner owner)
        {
            if (owner.TryGetModule(out IPlayerUpgraderModule upgraderModule))
                upgraderModule.SetUpgraderActive(upgrader, true);

            if (!owner.TryGetModule(out IStatModule statModule)) return;

            foreach (StatBonus bonus in statBonuses)
            {
                if (statModule.TryGetStat(bonus.Stat, out Stat stat))
                    stat.SetModify(this, bonus.Modify);
            }
        }

        public override void AgentDowngrade(ModuleOwner owner)
        {
            if (owner.TryGetModule(out IPlayerUpgraderModule upgraderModule))
                upgraderModule.SetUpgraderActive(upgrader, false);

            if (!owner.TryGetModule(out IStatModule statModule)) return;

            foreach (StatBonus bonus in statBonuses)
            {
                if (statModule.TryGetStat(bonus.Stat, out Stat stat))
                    stat.RemoveModify(this);
            }
        }
    }
}
