using System.Collections.Generic;

namespace _Scripts.Agent.HealthSystem.NewHealthSystem
{
    // 여러 겹으로 된 체력(코어 체력 + 추가 체력)을 읽기 전용으로 내보낸다.
    // 체력 UI처럼 겹마다 따로 그려야 하는 쪽이 쓴다. NewHealthModule이 구현한다.
    public interface IHealthLayers
    {
        // 우선순위 순서의 체력 목록.
        IReadOnlyList<IHealth> Healths { get; }

        // 체력을 만든 SO(글자 색 같은 표시 설정이 들어 있다).
        AbstractHealthSo GetHealthSo(IHealth health);

        // 피해/회복으로 전체 체력이 바뀌었을 때.
        event HealthChanged OnHealthChanged;
    }
}
