using System;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.EffectSystem;
using UnityEngine;

namespace Lrw.Script.Test
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