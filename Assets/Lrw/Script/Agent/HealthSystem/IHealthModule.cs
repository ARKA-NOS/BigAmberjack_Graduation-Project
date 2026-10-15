using System;

namespace Lrw.Script.Agent.HealthSystem
{
    public interface IHealthModule
    {
        float CurrentHealth { get; set; }
        float MaxHealth { get; }
        event HealthModule.HealthChanged OnHealthChanged;
        event Action OnDead;
        bool IsDead { get; }
    }
}