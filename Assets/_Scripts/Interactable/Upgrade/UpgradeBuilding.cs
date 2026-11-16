using _Scripts.Agent.Players;
using _Scripts.Agent.Players.CharacterSystem;
using _Scripts.DevLib.ModuleSystem;
using _Scripts.UI.UI_System;

namespace _Scripts.Interactable.Upgrade
{
    public class UpgradeBuilding : WindowOpenObject
    {
        public override void Interact(ModuleOwner owner)
        {
            CharacterData data = (owner as PlayerController)?.CharacterData;
            UIManagerEvents.OpenCloseWindow(TargetWindow, data);
        }
    }
}