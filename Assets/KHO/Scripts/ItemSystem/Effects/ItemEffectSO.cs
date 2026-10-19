using DevLib.ModuleSystem;
using UnityEngine;

namespace KHO.Scripts.ItemSystem.Effects
{
    // SO는 모든 아이템이 공유하므로 런타임 상태를 필드에 저장하지 않는다.
    // key는 효과를 적용한 주체(ItemInstance 등)로, 같은 아이템을 중복 장착해도 따로 적용·해제하기 위해 쓴다.
    public abstract class ItemEffectSO : ScriptableObject
    {
        public abstract void Apply(ModuleOwner owner, object key);
        public abstract void Remove(ModuleOwner owner, object key);
    }
}
