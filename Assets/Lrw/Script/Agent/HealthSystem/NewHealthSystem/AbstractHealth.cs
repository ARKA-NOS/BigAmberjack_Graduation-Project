using System;
using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    public abstract class AbstractHealth : IHealth
    {
        protected readonly ModuleOwner Owner;
        
        public Color CurrentColor { get; protected set; }
        
        public AbstractHealth(ModuleOwner owner, Color color)
        {
            Owner = owner;
            CurrentColor = color;
        }
        
        public float HP { get; protected set; }
        
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