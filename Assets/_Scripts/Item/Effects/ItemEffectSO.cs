using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Item.Effects
{
    public abstract class ItemEffectSO : ScriptableObject
    {
        public abstract void Apply(ModuleOwner owner, object key);
        public abstract void Remove(ModuleOwner owner, object key);
    }
}
