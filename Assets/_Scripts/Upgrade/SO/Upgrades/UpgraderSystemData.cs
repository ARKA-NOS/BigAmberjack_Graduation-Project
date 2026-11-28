using _Scripts.DevLib.ModuleSystem;
using _Scripts.Upgrade.Upgrader;
using UnityEngine;

namespace _Scripts.Upgrade.SO.Upgrades
{
    [CreateAssetMenu(fileName = "Upgrader System Data", menuName = "Upgrade/Upgrade Data/Upgrader System Data", order = 0)]
    public class UpgraderSystemData : AbstractUpgradeData
    {
        [SerializeField] private UpgraderSO upgrader;

        public override void AgentUpgrade(ModuleOwner owner)
        {
            if (!owner.TryGetModule(out IPlayerUpgraderModule upgraderModule)) return;

            upgraderModule.SetUpgraderActive(upgrader, true);
        }

        public override void AgentDowngrade(ModuleOwner owner)
        {
            if (!owner.TryGetModule(out IPlayerUpgraderModule upgraderModule)) return;

            upgraderModule.SetUpgraderActive(upgrader, false);
        }
    }
}
