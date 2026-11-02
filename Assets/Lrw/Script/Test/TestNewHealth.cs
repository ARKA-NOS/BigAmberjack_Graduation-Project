using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using UnityEngine;

namespace Lrw.Script.Test
{
    public class TestNewHealth : MonoBehaviour
    {
        [SerializeField] private NewHealthModule healthModule;
        [SerializeField] private float value = 5f;
        [SerializeField] private HealthType target = HealthType.None;
        [ContextMenu("Apply Damage")]
        public void ApplyDamage()
        {
            healthModule.ApplyDamage(value,target);
        }

        [ContextMenu("Apply Heal")]
        public void ApplyHeal()
        {
            healthModule.ApplyHeal(value,target);
        }
        
    }
}