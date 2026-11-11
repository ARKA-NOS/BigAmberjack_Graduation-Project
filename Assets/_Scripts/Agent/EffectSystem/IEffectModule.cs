using System.Collections.Generic;

namespace _Scripts.Agent.EffectSystem
{
    // 버프/디버프를 받고, 지금 걸려 있는 효과를 읽기 전용으로 내보낸다. EffectModule이 구현한다.
    public interface IEffectModule
    {
        // 지금 걸려 있는 효과. 같은 효과 데이터끼리 묶여 있다. 읽기만 한다.
        IReadOnlyDictionary<AbstractEffectDataSo, List<IEffect>> Effects { get; }

        void TakeEffect(AbstractEffectDataSo effectDataSo);
    }
}
