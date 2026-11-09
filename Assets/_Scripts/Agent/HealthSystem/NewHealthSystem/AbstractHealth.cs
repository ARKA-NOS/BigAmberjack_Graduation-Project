using System;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.HealthSystem.NewHealthSystem
{
    public abstract class AbstractHealth : IHealth
    {
        protected readonly ModuleOwner Owner;
        public int Priority { get; private set; }
        public AbstractHealth(int priority, ModuleOwner owner)
        {
            Priority = priority;
            Owner = owner;
        }

        public float HP { get; protected set; } = 0f;
        
        public abstract Color GetHealthColor();
        public abstract HealthType GetHealthType();
        public virtual float ApplyDamage(float damage)
        {
            float a = MathF.Min(HP, damage);
            HP -= a;
            return damage - a;
        }

        public virtual float ApplyHeal(float heal)
        {
            HP += heal;
            return 0f;
        }

        public virtual void SetHp(float hp)
        {
            HP = hp;
        }

        public virtual void HealthUpdate() {}
    }
}