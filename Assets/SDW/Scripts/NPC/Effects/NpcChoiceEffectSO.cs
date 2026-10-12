using DevLib.ModuleSystem;
using UnityEngine;

namespace SDW.Scripts.NPC.Effects
{
    // 선택지를 골랐을 때 실제로 일어나는 일(보상, 손실, 버프, 다음 방 영향 등)의 부모 SO.
    // 효과 종류마다 이 클래스를 상속한 자식 SO를 만들고 NpcChoiceData.Effects에 넣는다.
    // owner는 상호작용한 플레이어. 스탯 효과는 owner.GetModule<IStatModule>()로 접근한다.
    public abstract class NpcChoiceEffectSO : ScriptableObject
    {
        public abstract bool CanApply(ModuleOwner owner);
        public abstract void Apply(ModuleOwner owner);
    }
}
