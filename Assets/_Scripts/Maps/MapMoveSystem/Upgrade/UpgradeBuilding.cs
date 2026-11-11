using _Scripts.Agent.Players.InteractSystem;
using _Scripts.DevLib.ModuleSystem;
using _Scripts.UI.UI_System;
using _Scripts.UI.UpgradeUI;
using _Scripts.Upgrade.SO;
using UnityEngine;

namespace _Scripts.Maps.MapMoveSystem.Upgrade
{
    public class UpgradeBuilding : AbstractInteractObject
    {
        [SerializeField] private UpgradeTreeWindow testUI;
        
        [SerializeField] private UpgradeTreeSO treeData;
        
        public override void Interact(ModuleOwner owner)
        {
            UIManagerEvents.OpenCloseWindow(testUI,treeData);
        }
        
        
    }
}