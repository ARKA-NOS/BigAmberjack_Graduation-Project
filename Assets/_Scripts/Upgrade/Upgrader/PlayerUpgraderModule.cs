using System.Collections.Generic;
using _Scripts._Core._Debug;
using _Scripts.DevLib.ModuleSystem;

namespace _Scripts.Upgrade.Upgrader
{
    public class PlayerUpgraderModule : Module, IPlayerUpgraderModule, IAfterInitModule
    {
        private Dictionary<UpgraderSO, IUpgraderSystem> _upgraders = new();

        public void AfterInit()
        {
            _upgraders = GetUpgraders(GetComponentsInChildren<IUpgraderSystem>(true));
        }

        private Dictionary<UpgraderSO, IUpgraderSystem> GetUpgraders(IUpgraderSystem[] upgraders)
        {
            Dictionary<UpgraderSO, IUpgraderSystem> upgraderDict = new();

            foreach (IUpgraderSystem upgrader in upgraders)
            {
                if (upgrader.UpgraderSo == null)
                {
                    FDebug.LogError($"[{upgrader}] Upgrader SO가 null 입니다.");
                    continue;
                }

                if (!upgraderDict.TryAdd(upgrader.UpgraderSo, upgrader))
                {
                    FDebug.LogError($"[{upgrader}] 같은 Upgrader SO가 있습니다.");
                    continue;
                }

                upgrader.InitUpgrader(_owner);
            }

            return upgraderDict;
        }

        public IUpgraderSystem GetUpgrader(UpgraderSO upgraderSo)
            => upgraderSo != null ? _upgraders.GetValueOrDefault(upgraderSo) : null;

        public void SetUpgraderActive(UpgraderSO upgraderSo, bool active)
            => GetUpgrader(upgraderSo)?.SetActive(active);
    }
}
