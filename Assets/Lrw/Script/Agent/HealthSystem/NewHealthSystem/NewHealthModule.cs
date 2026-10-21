using System;
using DevLib.ModuleSystem;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    public class NewHealthModule : Module, IHealthModule
    {
        public float CurrentHealth { get; set; }
        public float MaxHealth { get; }
        public event HealthModule.HealthChanged OnHealthChanged;
        public event Action OnDead;
        public bool IsDead { get; private set; }
    }
}