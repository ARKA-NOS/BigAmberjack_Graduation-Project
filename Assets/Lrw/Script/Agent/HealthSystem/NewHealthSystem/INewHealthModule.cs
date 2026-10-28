using System;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    public delegate void HealthChanged(float newValue, float delta, float max);
    public interface INewHealthModule
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        bool IsDead { get; }
        event Action OnDead;
        event HealthChanged OnHealthChanged;

        void ApplyDamage(float damage, HealthType targets = HealthType.All);
        void ApplyHeal(float heal, HealthType targets = HealthType.Core);
    }
}