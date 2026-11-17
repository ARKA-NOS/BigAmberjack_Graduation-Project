using System;

namespace _Scripts.Agent.HealthSystem
{
    public delegate void HealthChanged(float newValue, float delta, float max);
    
    [Obsolete("Use INewHealthModule")]
    public interface IHealthModule
    {
        float CurrentHealth { get; set; }
        float MaxHealth { get; }
        event HealthChanged OnHealthChanged;
        event Action OnDead;
        bool IsDead { get; }
    }
}
