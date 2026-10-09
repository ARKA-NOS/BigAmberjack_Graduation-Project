using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.InteractSystem
{
    public interface ICanInteract
    {
        Vector2 Position { get; }

        void Interact(ModuleOwner owner);
        
    }
}