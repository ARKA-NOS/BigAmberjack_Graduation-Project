using System;
using DevLib.ModuleSystem;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    public abstract class AbstractHealth
    {
        public readonly AbstractHealthSo HealthSo;
        protected readonly ModuleOwner Owner;
        public float Hp { get; protected set; }
        public HealthType Type {get; private set;}

        protected AbstractHealth(AbstractHealthSo healthSo, ModuleOwner owner,float baseHp = 0)
        {
            HealthSo = healthSo;
            Owner = owner;
            Hp = baseHp;
        }

        public virtual void Init()
        {
            
        }
        
        public virtual bool UpdateHealth()
            => false;

        public virtual HealthChangeData ApplyDamage(HealthChangeData damage)
        {
            float taken = Mathf.Min(Hp, damage.Amount);
            Hp -= taken;
            damage.Amount -= taken;
            return damage;
        }
        
        public virtual HealthChangeData ApplyHeal(HealthChangeData heal)
        {
            if (heal.Amount < 0)
            {
                heal.Amount = 0f;
                return heal;
            }
            Hp += heal.Amount;
            heal.Amount = 0f;
            return heal;
        }
        
        public virtual void SetHp(float hp)
        {
            Hp = hp;
        }


        
    }
}
