using System;
using System.Collections.Generic;
using System.Linq;
using DevLib.BattleSystem;
using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.HealthSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Lumin;

namespace Lrw.Script.Agent
{
    public class Agent : ModuleOwner, IDamageable, IHealable
    {
        [SerializeField] protected UnityEvent OnHit;
        
        protected INewHealthModule NewHealthModule { get; private set; }

        private Dictionary<object, bool> invincibilityDict;
        
        protected override void Awake()
        {
            base.Awake();
            invincibilityDict = new();
            NewHealthModule = GetModule<INewHealthModule>();
            FDebug.Assert(NewHealthModule != null, $"[{name}] INewHealthModule is not found");
        }

        public void SetInvincibility(object obj, bool value)
        {
            invincibilityDict[obj] = value;
        }
        
        public virtual void ApplyDamage(DamageData damageData, Vector2 hitPoint, Vector2 hitDirection, Vector2 hitNormal)
        {
            if(invincibilityDict.Any(x => x.Value)) return;
            NewHealthModule.ApplyDamage(damageData.DamageAmount);
            OnHit?.Invoke();
        }
        
        public virtual void ApplyHeal(HealData healData)
        {
            NewHealthModule.ApplyHeal(healData.HealAmount);
        }
    }
}
