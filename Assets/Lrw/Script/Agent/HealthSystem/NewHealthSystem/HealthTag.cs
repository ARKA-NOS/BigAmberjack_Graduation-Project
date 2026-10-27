using System;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    /// <summary>
    /// 체력 분류. 피해의 IgnoreTags(보호막 관통 등)와 UI 묶음 조회에 쓴다.
    /// </summary>
    [Flags]
    public enum HealthTag
    {
        None = 0,
        Core = 1 << 0,
        Shield = 1 << 1,
        Armor = 1 << 2,
        Overheal = 1 << 3,
    }
}
