using System;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem;

namespace Lrw.Script.Agent.HealthSystem
{
    public delegate void HealthChanged(float newValue, float delta, float max);

    public interface IHealthModule
    {
        float CurrentHealth { get; set; }
        float MaxHealth { get; }
        event HealthChanged OnHealthChanged;
        event Action OnDead;
        bool IsDead { get; }

        HealthChangeResult TakeDamage(HealthChangeData data);
        HealthChangeResult Heal(HealthChangeData data);
    }
}
