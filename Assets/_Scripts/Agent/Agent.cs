using System.Collections.Generic;
using System.Linq;
using _Scripts._Core._Debug;
using _Scripts.Agent.HealthSystem.NewHealthSystem;
using _Scripts.DevLib.BattleSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;
using UnityEngine.Events;

namespace _Scripts.Agent
{
    public class Agent : ModuleOwner, IDamageable, IHealable
    {
        [SerializeField] protected UnityEvent OnHit;
        
        protected INewHealthModule NewHealthModule { get; private set; }

        private Dictionary<object, bool> invincibilityDict;
        public bool IsInvincibility => invincibilityDict.Any(x => x.Value);
        
        protected override void Awake()
        {
            base.Awake();
            invincibilityDict = new();
            NewHealthModule = GetModule<INewHealthModule>();
            FDebug.Assert(NewHealthModule != null, $"[{gameObject.name}] INewHealthModule is not found");
        }

        public void SetInvincibility(object obj, bool value)
        {
            invincibilityDict[obj] = value;
        }
        
        public virtual void ApplyDamage(DamageData damageData, Vector2 hitPoint, Vector2 hitDirection, Vector2 hitNormal)
        {
            if(IsInvincibility) return;
            NewHealthModule.ApplyDamage(damageData.DamageAmount);
            OnHit?.Invoke();
            AgentEvents.RaiseDamaged(this, damageData);
        }
        
        public virtual void ApplyHeal(HealData healData)
        {
            NewHealthModule.ApplyHeal(healData.HealAmount);
        }
    }
}
