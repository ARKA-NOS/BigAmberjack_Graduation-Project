using System;
using System.Collections.Generic;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    /// <summary>
    /// 기본 체력 위에 추가 체력(보호막 등)을 겹쳐 쓰는 체력 모듈
    /// </summary>
    public interface IHealthLayerModule
    {
        IHealth BaseHealth { get; }
        /// <summary> 기본 체력 → 추가 체력(Priority 오름차순) </summary>
        IReadOnlyList<IHealth> Healths { get; }
        float TotalHealth { get; }

        /// <summary> (체력, 변화량) </summary>
        event Action<IHealth, float> OnAnyHealthChanged;
        event Action<IHealth> OnHealthAdded;
        event Action<IHealth> OnHealthRemoved;

        /// <summary> 이미 있는 SO면 새로 만들지 않고 다시 채운다 </summary>
        IHealth AddHealth(AbstractHealthSo healthSo);
        /// <summary> 기본 체력은 제거할 수 없다 </summary>
        bool RemoveHealth(AbstractHealthSo healthSo);
        bool TryGetHealth(AbstractHealthSo healthSo, out IHealth health);
    }
}
