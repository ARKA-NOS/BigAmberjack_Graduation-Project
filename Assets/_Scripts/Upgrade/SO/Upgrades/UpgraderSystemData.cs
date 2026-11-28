using _Scripts._Core._Debug;
using _Scripts.DevLib.ModuleSystem;
using _Scripts.Upgrade.SO;
using Scripts.Upgrade.Upgrader;
using UnityEngine;

namespace Scripts.Upgrade.SO.Upgrades
{
    [CreateAssetMenu(fileName = "Mark Upgrade Data", menuName = "Upgrade/Upgrade Data/Mark Upgrade Data", order = 0)]
    public class UpgraderSystemData : AbstractUpgradeData
    {
        [SerializeField] private GameObject upgrader;

        public override void AgentUpgrade(ModuleOwner owner)
        {
            
        }

        private void OnValidate()
        {
            if (!upgrader.TryGetComponent(out IUpgraderSystem system))
            {
                upgrader = null;
                FDebug.LogWarning("UpgraderSystem이 아니입니다 UpgraderSystem를 추가 해줍시다");
            }
        }
    }
}
