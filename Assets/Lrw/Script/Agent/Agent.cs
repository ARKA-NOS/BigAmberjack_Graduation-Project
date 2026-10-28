using System;
using DevLib.BattleSystem;
using DevLib.ModuleSystem;
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
        
        [Obsolete("Use NewHealthModule")]
        //이거 이제 사용 안함
        protected IHealthModule HealthModule { get; private set; }
        
        protected INewHealthModule NewHealthModule { get; private set; }
        
        protected override void Awake()
        {
            base.Awake();
            HealthModule = GetModule<IHealthModule>(); // 애러 방지 코드
            
            NewHealthModule = GetModule<INewHealthModule>();
            Debug.Assert(NewHealthModule != null, $"[{name}] INewHealthModule is not found");
        }
        
        public virtual void ApplyDamage(DamageData damageData, Vector2 hitPoint, Vector2 hitDirection, Vector2 hitNormal)
        {
            NewHealthModule.ApplyDamage(damageData.DamageAmount);
            OnHit?.Invoke();
        }
        
        public virtual void ApplyHeal(HealData healData)
        {
            NewHealthModule.ApplyHeal(healData.HealAmount);
        }
    }
}
