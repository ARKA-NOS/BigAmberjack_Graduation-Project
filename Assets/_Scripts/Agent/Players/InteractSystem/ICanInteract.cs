using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Players.InteractSystem
{
    public interface ICanInteract
    {
        Vector2 Position { get; }

        void Interact(ModuleOwner owner);
        
    }
}