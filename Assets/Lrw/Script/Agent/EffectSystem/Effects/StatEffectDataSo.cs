using DevLib.ModuleSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Agent.EffectSystem.Effects
{
    [CreateAssetMenu(fileName = "Stat Effect", menuName = "Effect/Stat Effect")]
    public class StatEffectDataSo : AbstractEffectDataSo
    {
        [SerializeField] private StatData stat;
        [SerializeField] private StatModifyData modifyData;
        
        public override IEffect CreateEffect(ModuleOwner owner)
            => new StatEffect(Type, owner, stat, modifyData);
    }
}