using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Item.Effects
{
    public abstract class ItemEffectSO : ScriptableObject
    {
        public abstract void Apply(ModuleOwner owner, object key);
        public abstract void Remove(ModuleOwner owner, object key);

        // 툴팁에 보여줄 효과 한 줄 설명. 비어 있으면 툴팁에서 건너뛴다.
        public virtual string GetDescription() => string.Empty;
    }
}
