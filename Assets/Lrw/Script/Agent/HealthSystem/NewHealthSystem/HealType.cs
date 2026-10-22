using System;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    [Flags]
    public enum HealType
    {
        None = 0,
        Normal = 1 << 0,
        LifeSteal = 1 << 1,
    }
}
