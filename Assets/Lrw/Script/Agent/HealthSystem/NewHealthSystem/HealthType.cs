using System;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    [Flags]
    public enum HealthType
    {
        None = 0,
        Core = 1 << 0,
        Shield = 1 << 1,
        LifeStill = 1 << 2,
        All = Core | Shield | LifeStill
    }
}