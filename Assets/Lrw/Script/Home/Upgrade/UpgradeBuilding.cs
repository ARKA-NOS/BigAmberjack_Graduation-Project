using DevLib.ModuleSystem;
using Lrw.Script.Player.InteractSystem;
using Lrw.Script.UI;
using UnityEngine;

namespace Lrw.Script.Home.Upgrade
{
    public class UpgradeBuilding : AbstractInteractObject
    {
        [SerializeField] private AbstractWindow testUI;
        
        public override void Interact(ModuleOwner owner)
        {
            UIManagerEvents.OpenCloseWindow(testUI);
        }
        
        
        
    }
}