using System.Collections.Generic;
using DevLib.ModuleSystem;
using UnityEngine;

namespace SDW.Scripts.Interact
{
    // 선택지를 골랐을 때 실제로 일어나는 일(보상, 버프 등)의 부모 SO.
    // 효과 종류마다 이 클래스를 상속한 자식 SO를 만들고 NpcChoiceData.Effects나 ChestDataSO.Rewards에 넣는다.
    // owner는 상호작용한 플레이어. 스탯 효과는 owner.GetModule<IStatModule>()로 접근한다.
    // 비용(골드 소모, HP 소모 등)은 효과가 아니라 ChoiceCostSO로 만든다.
    public abstract class ChoiceEffectSO : ScriptableObject
    {
        public abstract bool CanApply(ModuleOwner owner);
        public abstract void Apply(ModuleOwner owner);

        // 하나라도 적용할 수 있는 효과가 있는지. 없으면(예: 인벤토리 가득 참) 상호작용 자체를 막아 보상이 사라지지 않게 한다.
        public static bool AnyApplicable(IEnumerable<ChoiceEffectSO> effects, ModuleOwner owner)
        {
            foreach (ChoiceEffectSO effect in effects)
            {
                if (effect != null && effect.CanApply(owner))
                    return true;
            }

            return false;
        }

        // 적용할 수 있는 효과만 골라서 적용한다.
        public static void ApplyAll(IEnumerable<ChoiceEffectSO> effects, ModuleOwner owner)
        {
            foreach (ChoiceEffectSO effect in effects)
            {
                if (effect != null && effect.CanApply(owner))
                    effect.Apply(owner);
            }
        }
    }
}
