using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.InteractSystem
{
    public abstract class AbstractInteractObject : MonoBehaviour, ICanInteract
    {
        public Vector2 Position => transform.position;
        public abstract void Interact(ModuleOwner owner);
        
    }
}