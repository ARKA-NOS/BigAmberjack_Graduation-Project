using Lrw.Script.Agent.EffectSystem;
using Lrw.Script.Agent.EffectSystem.Effects;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Test
{
    public class EffectTester : MonoBehaviour
    {
        [SerializeField] private EffectData data;
        [SerializeField] private EffectModule effectModule;
        
        [SerializeField] private StatData statData;
        
        private Effect _effect;
        
        [ContextMenu("Add")]
        private void TestAdd()
        {
            _effect = new PositionEffect(data, statData, 5f);
            effectModule.AddEffect(_effect);
        }
        
        [ContextMenu("Remove")]
        private void TestRemove()
        {
            effectModule.RemoveEffect(_effect);
        }
    }
}