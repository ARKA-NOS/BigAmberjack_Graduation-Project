using _Scripts.Agent.Players;
using _Scripts.DevLib.ModuleSystem;
using _Scripts.UI.UI_System;
using _Scripts.Upgrade.SO;

namespace _Scripts.Interactable.Upgrade
{
    public class UpgradeBuilding : WindowOpenObject
    {
        public override void Interact(ModuleOwner owner)
        {
            UpgradeTreeSO data = (owner as PlayerController)?.CharacterData.TreeSo;
            UIManagerEvents.OpenCloseWindow(TargetWindow, data);
        }
    }
}