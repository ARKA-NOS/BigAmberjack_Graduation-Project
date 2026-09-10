using DevLib.BattleSystem;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.HealthSystem;
using UnityEngine;
using UnityEngine.Events;

namespace Agents
{
    public class Agent : ModuleOwner,IDamageable
    {
        [SerializeField] protected UnityEvent OnHit;
        
        public IHealthModule HealthModule { get; private set; }
        
        protected override void Awake()
        {
            base.Awake();
            HealthModule = GetModule<IHealthModule>();
            Debug.Assert(HealthModule != null, $"[{name}] HealthModule is not found");
        }

        public void ApplyDamage(DamageData damageData, Vector2 hitPoint, Vector2 hitDirection, Vector2 hitNormal)
        {
            //임시
            HealthModule.CurrentHealth -= damageData.DamageAmount;
            OnHit?.Invoke();
        }
    }
}