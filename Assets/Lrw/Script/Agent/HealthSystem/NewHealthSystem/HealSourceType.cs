using System;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    /// <summary>
    /// 회복이 어디서 왔는지. 체력 SO의 HealSources에 들어 있는 출처의 회복만 그 체력을 채운다.
    /// </summary>
    [Flags]
    public enum HealSourceType
    {
        None = 0,
        Normal = 1 << 0,    // 아이템, 일반 회복
        LifeSteal = 1 << 1, // 흡혈
        Skill = 1 << 2,
        Regen = 1 << 3,     // 외부 재생 효과
        All = ~0,
    }
}
