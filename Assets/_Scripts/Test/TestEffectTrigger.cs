using _Scripts.Agent.EffectSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Test
{
    [RequireComponent(typeof(Collider2D))]
    public class TestEffectTrigger : MonoBehaviour
    {
        [SerializeField] private AbstractEffectDataSo effectDataSo;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out ModuleOwner owner)
                && owner.TryGetModule(out EffectModule effectModule))
            {
                effectModule.TakeEffect(effectDataSo);
            }
        }
        
    }
}