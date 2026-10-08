using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Upgrade
{
    //[CreateAssetMenu(fileName = "??? Upgrade Data", menuName = "Upgrade/Upgrade Data/??? Upgrade Data", order = 0)]
    public abstract class AbstractUpgradeData : ScriptableObject
    {
        public abstract void AgentUpgrade(ModuleOwner owner);
    }
}