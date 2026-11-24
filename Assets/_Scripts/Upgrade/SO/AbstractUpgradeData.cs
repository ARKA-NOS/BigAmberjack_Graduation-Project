using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Upgrade.SO
{
    //[CreateAssetMenu(fileName = "??? Upgrade Data", menuName = "Upgrade/Upgrade Data/??? Upgrade Data", order = 0)]
    public abstract class AbstractUpgradeData : ScriptableObject
    {
        public abstract void AgentUpgrade(ModuleOwner owner);

        // 구매가 취소(초기화)됐을 때 이미 적용한 효과를 되돌린다. 되돌릴 게 없으면 비워 둔다.
        public virtual void AgentDowngrade(ModuleOwner owner) { }
    }
}