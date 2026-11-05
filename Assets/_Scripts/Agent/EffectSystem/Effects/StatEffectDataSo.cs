using _Scripts.Agent.StatSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.EffectSystem.Effects
{
    [CreateAssetMenu(fileName = "Stat Effect", menuName = "Effect/Stat Effect")]
    public class StatEffectDataSo : AbstractEffectDataSo
    {
        [SerializeField] private float duration = 10f;
        [SerializeField] private StatData stat;
        [SerializeField] private StatModifyData modifyData;
        
        
        public override IEffect CreateEffect(ModuleOwner owner)
            => new StatEffect(owner, duration, stat, modifyData);
        
    }
}