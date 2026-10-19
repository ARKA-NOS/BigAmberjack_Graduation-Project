using DevLib.ModuleSystem;
using KHO.Scripts.ItemSystem.Effects;

namespace KHO.Scripts.ItemSystem
{
    // 인벤토리에 실제로 들어가는 아이템. 같은 ItemSO라도 인스턴스마다 효과가 따로 적용된다.
    public class ItemInstance
    {
        public ItemSO Data { get; }

        public ItemInstance(ItemSO data)
        {
            Data = data;
        }

        public void ApplyEffects(ModuleOwner owner)
        {
            foreach (ItemEffectSO effect in Data.Effects)
            {
                if (effect == null) continue;
                effect.Apply(owner, this);
            }
        }

        public void RemoveEffects(ModuleOwner owner)
        {
            foreach (ItemEffectSO effect in Data.Effects)
            {
                if (effect == null) continue;
                effect.Remove(owner, this);
            }
        }
    }
}
