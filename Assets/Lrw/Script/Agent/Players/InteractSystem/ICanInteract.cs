using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Player.InteractSystem
{
    public interface ICanInteract
    {
        Vector2 Position { get; }

        void Interact(ModuleOwner owner);
        
    }
}