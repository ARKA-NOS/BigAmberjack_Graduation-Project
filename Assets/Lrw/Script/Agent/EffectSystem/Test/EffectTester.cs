using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Agent.EffectSystem.Test
{
    public class EffectTester : MonoBehaviour
    {
        [SerializeField] private EffectModule effectModule;
        
        [SerializeField] private AbstractEffectSO effectSo;
        
        [ContextMenu("Set Effect")]
        public void SetEffect()
        {
            effectModule.Effect(effectSo);
        }
        
        
    }
}