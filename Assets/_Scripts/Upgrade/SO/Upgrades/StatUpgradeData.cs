using _Scripts.Agent.StatSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Upgrade.SO.Upgrades
{
    [CreateAssetMenu(fileName = "Stat Upgrade Data", menuName = "Upgrade/Upgrade Data/Stat Upgrade Data", order = 0)]
    public class StatUpgradeData : AbstractUpgradeData
    {
        [field: SerializeField] public StatData StatType { get; private set; }
        [field: SerializeField] public StatModifyData Modify { get; private set; }
        
        public override void AgentUpgrade(ModuleOwner owner)
        {
             if(!owner.TryGetModule(out IStatModule statModule)) return;
             
             Stat stat = statModule.GetStat(StatType);
             
             stat.SetModify(this,Modify);
        }
        
    }
}