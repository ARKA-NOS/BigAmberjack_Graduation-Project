using DevLib.ModuleSystem;
using UnityEngine;

namespace KHO.Scripts.ItemSystem.Effects
{
    public abstract class ItemEffectSO : ScriptableObject
    {
        public abstract void Apply(ModuleOwner owner, object key);
        public abstract void Remove(ModuleOwner owner, object key);
    }
}
