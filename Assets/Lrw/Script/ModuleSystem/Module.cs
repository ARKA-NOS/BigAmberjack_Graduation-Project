using UnityEngine;

namespace Script.ModuleSystem
{
    public abstract class Module : MonoBehaviour,IModule
    {
        protected ModuleOwner Owner { get; private set; }
        public virtual void Initialize(ModuleOwner owner)
        {
            Owner = owner;
        }
        
    }
}