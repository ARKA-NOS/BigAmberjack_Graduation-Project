using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.InteractSystem
{
    public abstract class AbstractInteractObject : MonoBehaviour, ICanInteract
    {
        public Vector2 Position => transform.position;
        public abstract void Interact(ModuleOwner owner);
        
    }
}