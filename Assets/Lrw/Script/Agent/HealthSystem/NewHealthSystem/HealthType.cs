using System;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    [Flags]
    public enum HealthType
    {
        None = 0,
        Normal = 1 << 0,
        Shield = 1 << 1,
        LifeSteal = 1 << 2,
        Armor = 1 << 3,
    }
}
