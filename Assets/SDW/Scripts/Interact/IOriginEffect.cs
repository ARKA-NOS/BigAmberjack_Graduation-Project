using DevLib.ModuleSystem;
using UnityEngine;

namespace SDW.Scripts.Interact
{
    // 발생 위치가 필요한 효과(코인 드랍 등)가 구현하는 인터페이스.
    // origin은 효과가 시작될 월드 좌표. 구현하지 않은 효과는 ChoiceEffectSO.Apply(owner)로 적용된다.
    public interface IOriginEffect
    {
        void Apply(ModuleOwner owner, Vector3 origin);
    }
}
