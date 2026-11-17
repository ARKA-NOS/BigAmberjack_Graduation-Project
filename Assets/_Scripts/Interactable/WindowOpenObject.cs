using _Scripts.Agent.Players.InteractSystem;
using _Scripts.DevLib.ModuleSystem;
using _Scripts.UI.UI_System;
using UnityEngine;

namespace _Scripts.Interactable
{
    public class WindowOpenObject : AbstractInteractObject
    {
        [field: SerializeField] protected AbstractWindow TargetWindow { get; private set; }
        
        public override void Interact(ModuleOwner owner)
        {
            UIManagerEvents.OpenCloseWindow(TargetWindow);
        }
        
    }
}