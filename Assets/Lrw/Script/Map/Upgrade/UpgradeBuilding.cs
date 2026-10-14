using DevLib.ModuleSystem;
using Lrw.Script.Agent.Players.InteractSystem;
using Lrw.Script.UI;
using Lrw.Script.UI.UpgradeUI;
using Lrw.Script.Upgrade.SO;
using UnityEngine;

namespace Lrw.Script.Map.Upgrade
{
    public class UpgradeBuilding : AbstractInteractObject
    {
        [SerializeField] private TestUpgradeUI testUI;
        
        [SerializeField] private UpgradeTreeSO treeData;
        
        public override void Interact(ModuleOwner owner)
        {
            UIManagerEvents.OpenCloseWindow(testUI,treeData);
        }
        
        
    }
}