using _Scripts.DataStructure;

namespace _Scripts.Agent.HealthSystem.NewHealthSystem.Healths
{
    public interface IRangeHealth
    {
        Gauge HpGauge { get; }
        bool ShowMaxHealth { get; }
    }
}