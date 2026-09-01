using DevLib.BattleSystem;

namespace Lrw.Script.Agent.HealthSystem
{
    public interface IHealthModule
    {
        float CurrentHealth { get; set; }
        float MaxHealth { get; }
        event HealthModule.HealthChanged OnHealthChanged;
    }
}