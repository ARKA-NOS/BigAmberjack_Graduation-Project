using Lrw.Script.DataStructure;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths
{
    public interface IRangeHealth
    {
        Gauge HpGauge { get; }
        bool ShowMaxHealth { get; }
    }
}