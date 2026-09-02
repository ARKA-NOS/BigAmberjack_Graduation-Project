using Lrw.Script._Core;
using Lrw.Script.Agent.HealthSystem;
using UnityEngine;

namespace Lrw.Script.Test
{
    public class TestDamage : MonoBehaviour
    {
        [SerializeField] private HealthModule healthModule;
        [SerializeField] private float value;

        [ContextMenu("Health Change")]
        private void OnHealthChanged()
        {
            healthModule.CurrentHealth += value;
            FDebug.Log(healthModule);
        }
    }
}