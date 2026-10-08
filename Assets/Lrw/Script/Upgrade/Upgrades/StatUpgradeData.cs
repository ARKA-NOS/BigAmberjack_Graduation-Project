using DevLib.ModuleSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Upgrade.Upgrades
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

             object obj = new object();
             stat.SetModify(obj,Modify);
        }
        
    }
}