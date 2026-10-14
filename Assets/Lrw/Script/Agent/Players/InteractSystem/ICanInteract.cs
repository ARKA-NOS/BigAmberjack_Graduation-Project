using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.InteractSystem
{
    public interface ICanInteract
    {
        Vector2 Position { get; }

        void Interact(ModuleOwner owner);
        
    }
}